using Godot;

namespace VolumetricRendering
{
    public class AppProgressView : IProgressView
    {
        public void StartProgress(string title, string description)
        {
        }

        public void FinishProgress(ProgressStatus status = ProgressStatus.Succeeded)
        {
        }

        public void UpdateProgress(float totalProgress, float currentStageProgress, string description)
        {
        }
    }
}