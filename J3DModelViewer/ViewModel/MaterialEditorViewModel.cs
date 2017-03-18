using JStudio.J3D;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace J3DModelViewer.ViewModel
{
    class MaterialEditorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Material CurrentMaterial
        {
            get { return m_currentMaterial; }
            set
            {
                m_currentMaterial = value;
                OnPropertyChanged();
            }
        }

        public ColorChannelControl CurrentColorChannelControl
        {
            get { return m_currentColorChannelControl; }
            set
            {
                m_currentColorChannelControl = value;
                OnPropertyChanged();
                OnPropertyChanged("CurrentColorChannelControlLightMaskLabel");
            }
        }

        public string CurrentColorChannelControlLightMaskLabel
        {
            get
            {
                return "Use Dropdown...";
            }
            set { }
        }

        private Material m_currentMaterial;
        private ColorChannelControl m_currentColorChannelControl;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
