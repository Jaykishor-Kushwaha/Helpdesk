namespace Helpdesk.DTOs
{
    // ✅ Generic ID DTO
    public class GetByIdDto
    {
        public int Id { get; set; }
    }

    public class GetAuditLogsByTicketDto
    {
        public int TicketId { get; set; }
    }

    public class GetAuditLogsByUserDto
    {
        public int UserId { get; set; }
    }

    // ✅ API RESPONSE (GENERIC)
    public class ApiResponse<T>
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public T? Data { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> FailResponse(string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default
            };
        }
    }

    // ✅ 🔥 IMPORTANT: OUTSIDE ApiResponse
    public class PagedResponse<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();

        public int Page { get; set; }
        public int PageSize { get; set; }

        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}