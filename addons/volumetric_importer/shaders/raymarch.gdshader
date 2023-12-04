shader_type spatial;
#include "TricubicSampling.gdshaderinc"
render_mode unshaded;

varying vec3 camera; //Local Camera position
varying vec3 position; //Vertex Position
varying vec2 uv;
varying mat4 view2clip_mtx;
varying mat4 local2view_mtx;

uniform float _MinVal = 0.;
uniform float _MaxVal = 1.;
uniform float _MinGradient = 0.;
uniform vec3 _TextureSize;
uniform float _LightingGradientThresholdStart = 0.0;
uniform float _LightingGradientThresholdEnd = 0.0;

uniform sampler3D volumeDataSampler;
uniform sampler3D volumeGradientSampler;
uniform sampler2D noiseSampler;
uniform sampler2D transferfunctionSamplerColor;
uniform sampler2D transferfunctionSamplerAlpha;
uniform bool useTransferFunction2D = false;
uniform bool useLighting = true;
uniform bool useMainLight = false;


uniform int MODE = 0; // 0 - MIP / 1 - DVR / 2 - IR

uniform bool earlyRayTermianation = true;

#define CUBIC_INTERPOLATION_ON true

uniform float noise_scale = 1.0;
uniform vec3 noise_transform;
uniform float density_threshold = 0.5;
uniform float deformation = 0.0;
uniform float anim_speed = 0.0;

uniform float volumeStepSize = 0.005;
uniform int volumeMaxSteps = 200;

#define JITTER_FACTOR 5.0
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
#if DEPTHWRITE_ON
    float depth;
#endif
};

vec3 getViewRayDir(vec3 vertexLocal)
{
	return normalize(vertexLocal - camera);
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
    ray.aabbInters = intersectAABB(ray.startPos, ray.direction, vec3(0.0, 0.0, 0.0), vec3(1.0f, 1.0f, 1.0));

    // Check if camera is inside AABB
    vec3 farPos = ray.startPos + ray.direction * ray.aabbInters.y - vec3(0.5f, 0.5f, 0.5f);
    vec4 farPos_view = local2view_mtx * vec4(farPos, 1.0);
	vec4 clipPos = view2clip_mtx * farPos_view;
    // TODO: vec4 clipPos = UnityObjectToClipPos(vec4(farPos, 1.0f));
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

float spaceWarp(vec3 p) {
	return pow(2.0 * length(p), deformation)/ 2.0;
}

vec3 random3D(vec3 uvw){
	
    uvw = vec3( dot(uvw, vec3(127.1,311.7, 513.7) ),
               dot(uvw, vec3(269.5,183.3, 396.5) ),
			   dot(uvw, vec3(421.3,314.1, 119.7) ) );
			
    return -1.0 + 2.0 * fract(sin(uvw) * 43758.5453123);
}

float noise3D(vec3 uvw){
	uvw *= spaceWarp(uvw) * noise_scale;
	uvw += vec3(noise_transform.x + sin(TIME * anim_speed),
				noise_transform.y + sin(TIME * anim_speed + 0.5),
				noise_transform.z + sin(TIME * anim_speed + 1.5));
	
	vec3 gridIndex = floor(uvw); 
	vec3 gridFract = fract(uvw);
	
	vec3 blur = smoothstep(0.0, 1.0, gridFract);
	
	vec3 blb = gridIndex + vec3(0.0, 0.0, 0.0);
	vec3 brb = gridIndex + vec3(1.0, 0.0, 0.0);
	vec3 tlb = gridIndex + vec3(0.0, 1.0, 0.0);
	vec3 trb = gridIndex + vec3(1.0, 1.0, 0.0);
	vec3 blf = gridIndex + vec3(0.0, 0.0, 1.0);
	vec3 brf = gridIndex + vec3(1.0, 0.0, 1.0);
	vec3 tlf = gridIndex + vec3(0.0, 1.0, 1.0);
	vec3 trf = gridIndex + vec3(1.0, 1.0, 1.0);
	
	vec3 gradBLB = random3D(blb); 
	vec3 gradBRB = random3D(brb);
	vec3 gradTLB = random3D(tlb);
	vec3 gradTRB = random3D(trb);
	vec3 gradBLF = random3D(blf);
	vec3 gradBRF = random3D(brf);
	vec3 gradTLF = random3D(tlf);
	vec3 gradTRF = random3D(trf);
	
	
	vec3 distToPixelFromBLB = gridFract - vec3(0.0, 0.0, 0.0);
	vec3 distToPixelFromBRB = gridFract - vec3(1.0, 0.0, 0.0);
	vec3 distToPixelFromTLB = gridFract - vec3(0.0, 1.0, 0.0);
	vec3 distToPixelFromTRB = gridFract - vec3(1.0, 1.0, 0.0);
	vec3 distToPixelFromBLF = gridFract - vec3(0.0, 0.0, 1.0);
	vec3 distToPixelFromBRF = gridFract - vec3(1.0, 0.0, 1.0);
	vec3 distToPixelFromTLF = gridFract - vec3(0.0, 1.0, 1.0);
	vec3 distToPixelFromTRF = gridFract - vec3(1.0, 1.0, 1.0);
	
	float dotBLB = dot(gradBLB, distToPixelFromBLB);
	float dotBRB = dot(gradBRB, distToPixelFromBRB);
	float dotTLB = dot(gradTLB, distToPixelFromTLB);
	float dotTRB = dot(gradTRB, distToPixelFromTRB);
	float dotBLF = dot(gradBLF, distToPixelFromBLF);
	float dotBRF = dot(gradBRF, distToPixelFromBRF);
	float dotTLF = dot(gradTLF, distToPixelFromTLF);
	float dotTRF = dot(gradTRF, distToPixelFromTRF);
	
	
	return mix(
		mix(
			mix(dotBLB, dotBRB, blur.x),
			mix(dotTLB, dotTRB, blur.x), blur.y
		),
		mix(
			mix(dotBLF, dotBRF, blur.x),
			mix(dotTLF, dotTRF, blur.x), blur.y
		), blur.z
	) + 0.5;
}

vec3 GetNormal(vec3 p){
	
	vec2 e = vec2(1e-2, 0);
	
	vec3 n = noise3D(p) - vec3(
		noise3D(p - e.xyy),
		noise3D(p - e.yxy),
		noise3D(p - e.yyx)
	);
	return normalize(n);
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
	#if defined(USE_MAIN_LIGHT)
    // TODO: return normalize(mul(unity_WorldToObject, _WorldSpaceLightPos0.xyz));
	#else
    return viewDir;
	#endif
}

// Performs lighting calculations, and returns a modified colour.
vec3 calculateLighting(vec3 col, vec3 normal, vec3 lightDir, vec3 eyeDir, float specularIntensity)
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
    return diffuse + specular;
}

frag_out frag_mip(vec3 vertexLocal) {
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

    RayInfo ray = getRayBack2Front(vertexLocal);
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
#if defined(LIGHTING_ON)
        float factor = smoothstep(_LightingGradientThresholdStart, _LightingGradientThresholdEnd, gradMag);
        vec3 shaded = calculateLighting(src.rgb, gradient / gradMag, getLightDirection(-ray.direction), -ray.direction, 0.3f);
        src.rgb = mix(src.rgb, shaded, factor);
#endif

        src.rgb *= src.a;
        col = (1.0f - col.a) * src + col;

        if (col.a > 0.15 && t < tDepth) {
            tDepth = t;
        }

        // Early ray termination
#if RAY_TERMINATE_ON
        if (col.a > OPACITY_THRESHOLD) {
            break;
        }
#endif
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
	RayInfo ray = getRayBack2Front(vertexLocal);
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
			    col.rgb = calculateLighting(col.rgb, normal, getLightDirection(-ray.direction), -ray.direction, 0.15);
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


void vertex() {
	uv = UV;
	position = VERTEX;
	camera = (inverse(MODELVIEW_MATRIX) * vec4(0,0,0,1)).xyz; //Camera Position in Local Space
    local2view_mtx = MODELVIEW_MATRIX;
    view2clip_mtx = PROJECTION_MATRIX;
	
}

void fragment() {
	vec3 ws_pixel_pos = ((INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz); //Pixel Position in World Space
	vec3 ls_pixel_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	vec3 ro = CAMERA_POSITION_WORLD; //Camera Position in World Space
	vec3 rd = normalize(ws_pixel_pos - ro);
	frag_out output = frag_dvr(position);
	
	ALBEDO = output.albedo;
	ALPHA = output.alpha;
	NORMAL_MAP_DEPTH = output.depth;
}