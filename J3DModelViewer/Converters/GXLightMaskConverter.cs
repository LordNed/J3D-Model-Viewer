using JStudio.J3D;
using System;
using System.Globalization;
using System.Windows.Data;

namespace J3DModelViewer.Converters
{
    public class GXLightMaskConverter : IValueConverter
    {
        private GXLightMask m_target;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            GXLightMask mask = (GXLightMask)parameter;
            this.m_target = (GXLightMask)value;
            return ((mask & this.m_target) != 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            this.m_target ^= (GXLightMask)parameter;
            return this.m_target;
        }
    }
}
