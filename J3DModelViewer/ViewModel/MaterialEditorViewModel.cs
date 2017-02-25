using JStudio.J3D;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
            }
        }
        private Material m_currentMaterial;
        private ColorChannelControl m_currentColorChannelControl;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
