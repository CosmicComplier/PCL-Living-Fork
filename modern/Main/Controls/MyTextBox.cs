using System;
using System.Collections.Generic;
using System.Windows;
using MeloongCore;
using PCLCS.Validation;
using PCLLF.Presentation.Controls;

namespace PCL
{
    public partial class MyTextBox : MyTextBoxView, ISettingControl
    {
        public MyTextBox()
        {
            __LfAttachOwnHandles();
            this._ValidateRules = new System.Collections.ObjectModel.Collection<Validate>();
        }

        static MyTextBox()
        {
            PresentationRuntime.EnsureConfigured();
        }

        public static new readonly DependencyProperty CornerRadiusProperty = MyTextBoxView.CornerRadiusProperty;
        public static new readonly DependencyProperty ValidateResultProperty = MyTextBoxView.ValidateResultProperty;
        public static new readonly DependencyProperty IsValidatedProperty = MyTextBoxView.IsValidatedProperty;
        public static new readonly DependencyProperty HintTextProperty = MyTextBoxView.HintTextProperty;
        public static new event ValidateChangedEventHandler ValidateChanged;
        public new delegate void ValidateChangedEventHandler(object sender, EventArgs e);
        protected override void OnValidateChanged()
        {
            ValidateChanged?.Invoke(this, new EventArgs());
        }

        private System.Collections.ObjectModel.Collection<Validate> _ValidateRules;
        public new System.Collections.ObjectModel.Collection<Validate> ValidateRules
        {
            get
            {
                return _ValidateRules;
            }

            set
            {
                _ValidateRules = value;
                Validate();
            }
        }

        protected override IEnumerable<LegacyValidationRule> ReadValidateRules()
        {
            return _ValidateRules;
        }

        public new event RoutedEventHandler ValidatedTextChanged
        {
            add
            {
                ChangedEventList.Add(value);
            }

            remove
            {
                ChangedEventList.Remove(value);
            }
        }

        void OnValidatedTextChanged(object sender, RoutedEventArgs e)
        {
            foreach (RoutedEventHandler Handler in ChangedEventList)
            {
                if (Handler is not null)
                    Handler(sender, e);
            }
        }

        protected override void UiPost(Action Work)
        {
            ModBase.RunInUi(Work);
        }

        protected override void UiWait(Action Work)
        {
            ModBase.RunInUiWait(Work);
        }

        protected override void StartWorker(Action Work, string Name)
        {
            ModBase.RunInNewThread(Work, Name);
        }

        protected override void ReportError(Exception ErrorValue, string Message)
        {
            Logger.Error(ErrorValue, Message);
        }

        protected override ControlColor ReadTextColor(bool Enabled)
        {
            var Value = Enabled ? ModSecret.ColorGray1 : ModSecret.ColorGray4;
            return new ControlColor(Value.A, Value.R, Value.G, Value.B);
        }

        private void RefreshSetting(string NewValue)
        {
            Text = NewValue;
        }

        void ISettingControl.RefreshSetting(string NewValue) => RefreshSetting(NewValue);
        private string GetCurrentSetting()
        {
            return Text;
        }

        string ISettingControl.GetCurrentSetting() => GetCurrentSetting();
        private void SaveSetting()
        {
            SettingService.SaveSetting(this);
        }
    }
}
