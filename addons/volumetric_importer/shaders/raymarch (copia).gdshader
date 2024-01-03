shader_type spatial;
#include "TricubicSampling.gdshaderinc"
//render_mode unshaded;
//render_mode cull_back;
//render_mode depth_test_disabled;

varying vec3 camera; //Local Camera position
varying vec3 position; //Vertex Position
varying vec2 uv;
varying mat4 MVP_MATRIX;
varying vec3 LIGHTT;

uniform float _MinVal = 0.001;
uniform float _MaxVal = 1.;
uniform float _MinGradient = 0.;
uniform vec3 _TextureSize;
uniform float _LightingGradientThresholdStart = 0.0;
uniform float _LightingGradientThresholdEnd = 0.0;
uniform bool useTransferFunction2D = false;
uniform bool useLighting = true;
uniform bool useMainLight = true;
uniform bool earlyRayTermianation = true;

uniform sampler3D volumeDataSampler;
uniform sampler3D volumeGradientSampler;
uniform sampler2D noiseSampler;
uniform sampler2D transferfunctionSamplerColor;
uniform sampler2D transferfunctionSamplerAlpha;


uniform int MODE = 0; // 0 - MIP / 1 - DVR / 2 - IR

#define CUBIC_INTERPOLATION_ON false
#define JITTER_FACTOR 1.0
#define AMBIENT_LIGHTING_FACTOR 0.5
#define DEPTHWRITE_ON true
#define LIGHTING_ON true
#define TF2D_ON false
#define RAY_TERMINATE_ON true

struct RayInfo
{
    vec3 startPos; //origin
    vec3 endPos;
    vec3 direction;
    vec2 aabbInters;
};
struct RaymarchInfo
{
    RayInfo ray;
    int numSteps;
    float numStepsRecip;
    float stepSize;
};
struct frag_out
{
    vec3 albedo;
    float alpha;
};
struct lightning_out
{
    vec3 diffuse;
	vec3 specular;
};

vec3 getViewRayDir(vec3 vertexLocal)
{
    return normalize(camera - vertexLocal);
}

// Find ray intersection points with axis aligned bounding box
vec2 intersectAABB(vec3 rayOrigin, vec3 rayDir, vec3 boxMin, vec3 boxMax)
{
    vec3 tMin = (boxMin - rayOrigin) / rayDir;
    vec3 tMax = (boxMax - rayOrigin) / rayDir;
    vec3 t1 = min(tMin, tMax);
    vec3 t2 = max(tMin, tMax);
    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);
    return vec2(tNear, tFar);
}

// Get a ray for the specified fragment (back-to-front)
RayInfo getRayBack2Front(vec3 vertexLocal)
{
    RayInfo ray;
    ray.direction = getViewRayDir(vertexLocal);
    ray.startPos = vertexLocal + vec3(0.5f, 0.5f, 0.5f);
    // Find intersections with axis aligned boundinng box (the volume)
    ray.aabbInters = intersectAABB(ray.startPos, ray.direction, vec3(0.0, 0.0, 0.0), vec3(1.0f, 1.0f, 1.0f));

    // Check if camera is inside AABB
    vec3 farPos = vertexLocal + ray.direction * ray.aabbInters.y;
    vec4 clipPos = MVP_MATRIX * vec4(farPos, 1.0);
    ray.aabbInters += min(clipPos.w, 0.0);

    ray.endPos = ray.startPos + ray.direction * ray.aabbInters.y;
    return ray;
}

// Get a ray for the specified fragment (front-to-back)
RayInfo getRayFront2Back(vec3 vertexLocal)
{
    RayInfo ray = getRayBack2Front(vertexLocal);
    ray.direction = -ray.direction;
    vec3 tmp = ray.startPos;
    ray.startPos = ray.endPos;
    ray.endPos = tmp;
    return ray;
}

RaymarchInfo initRaymarch(RayInfo ray, int maxNumSteps)
{
    RaymarchInfo raymarchInfo;
    raymarchInfo.stepSize = 1.732f/*greatest distance in box*/ / float(maxNumSteps);
    raymarchInfo.numSteps = int(clamp(abs(ray.aabbInters.x - ray.aabbInters.y) / raymarchInfo.stepSize, 1., float(maxNumSteps)));
    raymarchInfo.numStepsRecip = 1.0 / float(raymarchInfo.numSteps);
    return raymarchInfo;
}

// Gets the colour from a 1D Transfer Function (x = density)
vec4 getTF1DColour(float density)
{
    return vec4(texture(transferfunctionSamplerColor, vec2(density, 0.0f)).rgb, texture(transferfunctionSamplerAlpha, vec2(density, 0.0f)).a);
}

// Gets the gradient at the specified position
vec3 getGradient(vec3 pos)
{
    #if CUBIC_INTERPOLATION_ON
    return interpolateTricubicFast(volumeGradientSampler, pos, _TextureSize).rgb;
    #else
    vec3 gradient = texture(volumeGradientSampler, pos).rgb;
    return gradient;
    #endif
}
// Get the light direction (using main light or view direction, based on setting)
vec3 getLightDirection(vec3 viewDir)
{
    if (useMainLight)
	{
		return normalize(LIGHTT);
	}
    // TODO: return normalize(mul(unity_WorldToObject, _WorldSpaceLightPos0.xyz));
	else
	{
		return viewDir;
	}
}

// Performs lighting calculations, and returns a modified colour.
lightning_out calculateLighting(vec3 col, vec3 normal, vec3 lightDir, vec3 eyeDir, float specularIntensity)
{
    // Invert normal if facing opposite direction of view direction.
    // Optimised version of: if(dot(normal, eyeDir) < 0.0) normal *= -1.0
    normal *= (step(0.0, dot(normal, eyeDir)) * 2.0 - 1.0);

    float ndotl = max(mix(0.0f, 1.5f, dot(normal, lightDir)), AMBIENT_LIGHTING_FACTOR);
    vec3 diffuse = ndotl * col;
    vec3 v = eyeDir;
    vec3 r = normalize(reflect(-lightDir, normal));
    float rdotv = max( dot( r, v ), 0.0 );
    vec3 specular = pow(rdotv, 32.0f) * vec3(1.0f, 1.0f, 1.0f) * specularIntensity;
    lightning_out out_l;
    out_l.diffuse = diffuse;
    out_l.specular = specular;
    return out_l;
}

frag_out frag_mip(vec3 vertexLocal)
{
    int MAX_NUM_STEPS = 512;
    RayInfo ray = getRayBack2Front(vertexLocal);
    RaymarchInfo raymarchInfo = initRaymarch(ray, MAX_NUM_STEPS);
    float maxDensity = 0.0f;
    vec3 maxDensityPos = ray.startPos;
    for (int iStep = 0; iStep < raymarchInfo.numSteps; iStep++)
    {
        float t = float(iStep) * raymarchInfo.numStepsRecip;
        vec3 currPos = mix(ray.startPos, ray.endPos, t);
        
        #ifdef CROSS_SECTION_ON
        if (IsCutout(currPos))
            continue;
        #endif

        //float density = texture(volumeDataSampler, currPos).r;
        float density = texture(volumeDataSampler, currPos).r;
        if (density > maxDensity && density > _MinVal && density < _MaxVal)
        {
            maxDensity = density;
            maxDensityPos = currPos;
        }
    }
    vec4 output_colour = vec4(1.0f, 1.0f, 1.0f, maxDensity); // maximum intensity
    frag_out output;
    output.albedo = output_colour.xyz;
    output.alpha = output_colour.w;
    return output;
}

frag_out frag_dvr(vec3 vertexLocal)
{
    int MAX_NUM_STEPS = 512;
    #define OPACITY_THRESHOLD (1.0 - 1.0 / 255.0)

    RayInfo ray = getRayFront2Back(vertexLocal);
    RaymarchInfo raymarchInfo = initRaymarch(ray, MAX_NUM_STEPS);

    // Create a small random offset in order to remove artifacts
    ray.startPos += (JITTER_FACTOR * ray.direction * raymarchInfo.stepSize) * texture(noiseSampler, vec2(uv.x, uv.y)).r;

    vec4 col = vec4(0.0f, 0.0f, 0.0f, 0.0f);
    float tDepth = raymarchInfo.numStepsRecip * float(raymarchInfo.numSteps - 1);
    for (int iStep = 0; iStep < raymarchInfo.numSteps; iStep++)
    {
        float t = float(iStep) * raymarchInfo.numStepsRecip;
        vec3 currPos = mix(ray.startPos, ray.endPos, t);

        // Perform slice culling (cross section plane)
#ifdef CROSS_SECTION_ON
        if(IsCutout(currPos))
               continue;
#endif

        // Get the dansity/sample value of the current position
        float density = texture(volumeDataSampler, currPos).r;

        // Apply visibility window
        if (density < _MinVal || density > _MaxVal) continue;

        // Calculate gradient (needed for lighting and 2D transfer functions)
#if (TF2D_ON || LIGHTING_ON)
        vec3 gradient = getGradient(currPos);
        float gradMag = length(gradient);
        float gradMagNorm = gradMag / 1.75f;
#endif

        // Apply 2D transfer function
#if TF2D_ON
        vec4 src = getTF2DColour(density, gradMagNorm);
        if (src.a == 0.0)
            continue;
#else   // Apply 1D transfer function
        vec4 src = getTF1DColour(density);
        if (src.a == 0.0)
            continue;
#endif

        // Apply lighting
        if (useLighting)
        {
            float factor = smoothstep(_LightingGradientThresholdStart, _LightingGradientThresholdEnd, gradMag);
            lightning_out shaded = calculateLighting(src.rgb, gradient / gradMag, getLightDirection(-ray.direction), -ray.direction, 0.3f);
            src.rgb = mix(src.rgb, shaded.diffuse + shaded.specular, factor);
        }

        src.rgb *= src.a;
        col = (1.0f - col.a) * src + col;

        if (col.a > 0.15 && t < tDepth) {
            tDepth = t;
        }

        // Early ray termination
        if (earlyRayTermianation && col.a > OPACITY_THRESHOLD) {
            break;
        }
    }

    // Write output
    frag_out output;
#if DEPTHWRITE_ON
    tDepth += (step(col.a, 0.0) * 1000.0); // Write large depth if no hit
    vec3 depthPos = mix(ray.startPos, ray.endPos, tDepth) - vec3(0.5f, 0.5f, 0.5f);
    //output.depth = localToDepth(depthPos);
#endif

    
    output.albedo = col.xyz;
    output.alpha = col.w;
    return output;
}
frag_out frag_light_dvr(vec3 vertexLocal)
{
    int MAX_NUM_STEPS = 512;

    RayInfo ray = getRayFront2Back(vertexLocal);
    RaymarchInfo raymarchInfo = initRaymarch(ray, MAX_NUM_STEPS);

    // Create a small random offset in order to remove artifacts
    ray.startPos += (JITTER_FACTOR * ray.direction * raymarchInfo.stepSize) * texture(noiseSampler, vec2(uv.x, uv.y)).r;

    vec4 col = vec4(0.0f, 0.0f, 0.0f, 0.0f);
    float tDepth = raymarchInfo.numStepsRecip * float(raymarchInfo.numSteps - 1);
    for (int iStep = 0; iStep < raymarchInfo.numSteps; iStep++)
    {
        float t = float(iStep) * raymarchInfo.numStepsRecip;
        vec3 currPos = mix(ray.startPos, ray.endPos, t);

        // Perform slice culling (cross section plane)
#ifdef CROSS_SECTION_ON
        if(IsCutout(currPos))
               continue;
#endif

        // Get the dansity/sample value of the current position
        float density = texture(volumeDataSampler, currPos).r;

        // Apply visibility window
        if (density < _MinVal || density > _MaxVal) continue;

        // Calculate gradient (needed for lighting and 2D transfer functions)
#if (TF2D_ON || LIGHTING_ON)
        vec3 gradient = getGradient(currPos);
        float gradMag = length(gradient);
        float gradMagNorm = gradMag / 1.75f;
#endif

        // Apply 2D transfer function
#if TF2D_ON
        vec4 src = getTF2DColour(density, gradMagNorm);
        if (src.a == 0.0)
            continue;
#else   // Apply 1D transfer function
        vec4 src = getTF1DColour(density);
        if (src.a == 0.0)
            continue;
#endif

        // Apply lighting
        if (useLighting)
        {
            float factor = smoothstep(_LightingGradientThresholdStart, _LightingGradientThresholdEnd, gradMag);
            lightning_out shaded = calculateLighting(src.rgb, gradient / gradMag, getLightDirection(-ray.direction), -ray.direction, 0.3f);
            src.rgb = mix(src.rgb, shaded.diffuse + shaded.specular, factor);
        }

        src.rgb *= src.a;
        col = (1.0f - col.a) * src + col;

        if (col.a > 0.15 && t < tDepth) {
            tDepth = t;
        }

        // Early ray termination
        if (earlyRayTermianation && col.a > OPACITY_THRESHOLD) {
            break;
        }
    }

    // Write output
    frag_out output;
#if DEPTHWRITE_ON
    tDepth += (step(col.a, 0.0) * 1000.0); // Write large depth if no hit
    vec3 depthPos = mix(ray.startPos, ray.endPos, tDepth) - vec3(0.5f, 0.5f, 0.5f);
    //output.depth = localToDepth(depthPos);
#endif

    
    output.albedo = col.xyz;
    output.alpha = col.w;
    return output;
}

frag_out frag_surf(vec3 vertexLocal) {
    int MAX_NUM_STEPS = 1024;
    RayInfo ray = getRayFront2Back(vertexLocal);
    RaymarchInfo raymarchInfo = initRaymarch(ray, MAX_NUM_STEPS);
    
    ray.startPos = ray.startPos + (JITTER_FACTOR * ray.direction * raymarchInfo.stepSize) * texture(noiseSampler, vec2(uv.x, uv.y)).r;
    vec4 col = vec4(0.0f, 0.0f, 0.0f, 0.0f);
    for (int iStep = 0; iStep < raymarchInfo.numSteps; iStep++)
    {
        float t = float(iStep) * raymarchInfo.numStepsRecip;
        vec3 currPos = mix(ray.startPos, ray.endPos, t);
        
        #ifdef CROSS_SECTION_ON
        if (IsCutout(currPos))
            continue;
        #endif

        //float density = texture(volumeDataSampler, currPos).r;
        float density =  texture(volumeDataSampler, currPos).r;
        if (density > _MinVal && density < _MaxVal)
        {
            vec3 gradient = getGradient(currPos);
            float gradMag = length(gradient);
            if (gradMag > _MinGradient)
            {
                vec3 normal = gradient / gradMag;
                col = getTF1DColour(density);
                lightning_out shaded = calculateLighting(col.rgb, normal, getLightDirection(-ray.direction), -ray.direction, 0.15);
                col.rgb = shaded.diffuse + shaded.specular;
                col.a = 1.0f;
                break;
            }
        }
    }
    frag_out output;
    output.albedo = col.xyz;
    output.alpha = col.w;
    return output;
}

void vertex()
{
    uv = UV;
    position = VERTEX;
    camera = (inverse(MODELVIEW_MATRIX) * vec4(0,0,0,1)).xyz; //Camera Position in Local Space
    MVP_MATRIX = PROJECTION_MATRIX * MODELVIEW_MATRIX;
}

void fragment()
{
    switch (MODE)
    {
        case 0:
            frag_out output = frag_mip(position);
            ALBEDO = output.albedo;
            ALPHA = output.alpha;
            break;
        case 1:
            frag_out output = frag_dvr(position);
            ALBEDO = output.albedo;
            ALPHA = output.alpha;
            break;
        case 2:
            frag_out output = frag_surf(position);
            ALBEDO = output.albedo;
            ALPHA = output.alpha;
            break;
    }
}
void light() {
    switch (MODE)
    {
        case 0:
            frag_out output = frag_mip(position);
            DIFFUSE_LIGHT += output.albedo;
            ALPHA = output.alpha;
            break;
        case 1:
            frag_out output = frag_light_dvr(position);
            DIFFUSE_LIGHT += output.albedo;
            SPECULAR_LIGHT;
            ALPHA = output.alpha;
            break;
        case 2:
            frag_out output = frag_surf(position);
            DIFFUSE_LIGHT += output.albedo;
            SPECULAR_LIGHT;
            ALPHA = output.alpha;
            break;
    }
}