namespace Entities.Dtos.Data
{
    // Bazadakı bir cədvəl haqqında qısa məlumat
    public class TableInfoDto
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public bool IsReadOnly { get; set; }
    }

    // Sütunun tipi paneldə uyğun sahə göstərmək üçün lazımdır
    public class ColumnInfoDto
    {
        public string Name { get; set; } = string.Empty;

        // text | number | decimal | bool | date | datetime
        public string Type { get; set; } = "text";
        public bool IsKey { get; set; }
        public bool IsRequired { get; set; }

        // Baza özü doldurur (Id kimi) — formada göstərilmir
        public bool IsGenerated { get; set; }
    }

    public class TableRowsDto
    {
        public string Table { get; set; } = string.Empty;
        public string KeyColumn { get; set; } = "Id";
        public bool IsReadOnly { get; set; }
        public List<ColumnInfoDto> Columns { get; set; } = new();
        public List<Dictionary<string, object?>> Rows { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class TableRowDto
    {
        // Sütun adı → dəyər. Göndərilməyən sütunlara toxunulmur.
        public Dictionary<string, object?> Values { get; set; } = new();
    }
}
