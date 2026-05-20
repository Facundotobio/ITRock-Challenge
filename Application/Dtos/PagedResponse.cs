namespace ITRockChallenge.Application.Dtos
{
    public record PagedResponse<T>(
     IEnumerable<T> Data,
     int PageNumber,
     int PageSize,
     int TotalRecords
 )
    {
        // Propiedad calculada dinámicamente para saber el total de páginas
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
    }
}
