using LinqToDB.Common;
using LinqToDB.Mapping;
using System.Globalization;

namespace Linq2dbPrimaryKeyDateTimeOffset.Test.Lib;
public class DateTimeOffsetConverterAttribute : ValueConverterAttribute {
    public DateTimeOffsetConverterAttribute() : base() {
        ValueConverter = new DateTimeConverter();
        ConverterType = typeof(DateTimeConverter);
    }
}
internal class DateTimeConverter : ValueConverter<DateTimeOffset, string> {
    private static readonly IFormatProvider _provider = CultureInfo.InvariantCulture.DateTimeFormat;
    public DateTimeConverter() : base(
        input => input.ToString("O", _provider),
        input => string.IsNullOrWhiteSpace(input) ? DateTimeOffset.MinValue : DateTimeOffset.ParseExact(input, "O", _provider),
        false) {

    }
}
