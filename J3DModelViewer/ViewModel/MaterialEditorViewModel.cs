using JStudio.J3D;
using System.ComponentModel;

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
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("CurrentMaterial"));
            }
        }

        private Material m_currentMaterial;
    }
}
