using Godot;

namespace VolumetricRendering
{
    public class EditorProgressView : IProgressView
    {
        private ProgressBar progressView;
        private RichTextLabel progressText;
        public EditorProgressView(ProgressBar progressView, RichTextLabel progressText)
        {
            this.progressView = progressView;
            this.progressText = progressText;
        }
        public void StartProgress(string title, string description)
        {
            progressView.MaxValue = 1.0f;
            progressView.Value = 0.0f;
        }

        public void FinishProgress(ProgressStatus status = ProgressStatus.Succeeded)
        {
            progressText.Text = "Finished.";
        }

        public void UpdateProgress(float totalProgress, float currentStageProgress, string description)
        {
            progressView.Value = totalProgress;
            progressText.Text = description;
        }
    }
}