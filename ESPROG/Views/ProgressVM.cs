using System.Collections.Generic;
using System.Windows.Media;

namespace ESPROG.Views
{
    internal class ProgressVM : BaseViewModel
    {
        public enum State
        {
            Running, Succeed, Fail
        }

        private static readonly Dictionary<State, (Brush Color, string Text, bool IsBusy)> StateDict = new()
        {
            { State.Running, (Brushes.LightSkyBlue, "Running", true) },
            { State.Succeed, (Brushes.LightGreen, "Succeed", false) },
            { State.Fail, (Brushes.LightPink, "Fail", false) }
        };

        private Brush color;
        public Brush Color
        {
            get => color;
            private set => SetProperty(ref color, value);
        }

        private string text;
        public string Text
        {
            get => text;
            private set => SetProperty(ref text, value);
        }

        private bool isBusy;
        public bool IsBusy
        {
            get => isBusy;
            private set => SetProperty(ref isBusy, value);
        }

        public void SetSate(State state)
        {
            Color = StateDict[state].Color;
            Text = StateDict[state].Text;
            IsBusy = StateDict[state].IsBusy;
        }

        public ProgressVM()
        {
            color = StateDict[State.Succeed].Color;
            text = StateDict[State.Succeed].Text;
            isBusy = StateDict[State.Succeed].IsBusy;
        }
    }
}
