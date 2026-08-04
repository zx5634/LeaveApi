namespace LeaveApi.Middleware;

public static class LeaveErrors
{
    public const string EndBeforeStart = "結束日期不得早於開始日期。";
    public const string PeriodOverlap = "該期間已有假單，不得重複申請。";
    public const string NotPending = "假單狀態不是待審核，無法核准。";
    public static string EmployeeNotFound(int id) => $"找不到 ID 為 {id} 的員工。";
    public static string LeaveNotFound(int id) => $"找不到 ID 為 {id} 的假單。";
}
