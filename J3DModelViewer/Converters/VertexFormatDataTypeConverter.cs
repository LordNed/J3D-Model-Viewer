using JStudio.J3D;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace J3DModelViewer.Converters
{
    public class VertexFormatDataTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            VertexFormat vf = value as VertexFormat;
            if (vf == null)
                throw new ArgumentException("value must be of type VertexFormat");

            if (vf.ArrayType == VertexArrayType.Color0 || vf.ArrayType == VertexArrayType.Color1)
                return vf.ColorDataType.ToString();

            return vf.DataType.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
