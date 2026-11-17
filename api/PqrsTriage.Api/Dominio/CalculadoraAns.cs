namespace PqrsTriage.Api.Dominio;

public static class CalculadoraAns
{
    private const long MsPorDia = 86_400_000;
    private static readonly TimeSpan HoraColombia = TimeSpan.FromHours(-5);
    private static readonly long DesfaseColombiaMs = (long)HoraColombia.TotalMilliseconds;

    public static DateTimeOffset Vence(long inicioMs, int diasHabiles, IReadOnlyCollection<string> festivos)
    {
        var festivosSet = festivos as ISet<string> ?? festivos.ToHashSet();
        var finMs = SumarTiempoHabil(inicioMs, diasHabiles * MsPorDia, festivosSet);
        return DateTimeOffset.FromUnixTimeMilliseconds(finMs).ToOffset(HoraColombia);
    }

    public static string VenceIso(long inicioMs, int diasHabiles, IReadOnlyCollection<string> festivos) =>
        Vence(inicioMs, diasHabiles, festivos).ToString("yyyy-MM-ddTHH:mm:sszzz");

    public static bool EsDiaHabil(long ms, ISet<string> festivos)
    {
        var fecha = DateTimeOffset.FromUnixTimeMilliseconds(ms).ToOffset(HoraColombia);
        var esFinDeSemana = fecha.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        return !esFinDeSemana && !festivos.Contains(fecha.ToString("yyyy-MM-dd"));
    }

    private static long SumarTiempoHabil(long inicioMs, long duracionMs, ISet<string> festivos)
    {
        var actual = inicioMs;
        var restante = duracionMs;
        while (restante > 0)
        {
            var finDelDia = InicioDelDia(actual) + MsPorDia;
            var consumido = EsDiaHabil(actual, festivos) ? Math.Min(restante, finDelDia - actual) : 0;
            restante -= consumido;
            actual = restante > 0 ? finDelDia : actual + consumido;
        }
        return actual;
    }

    private static long InicioDelDia(long ms) =>
        (ms + DesfaseColombiaMs) / MsPorDia * MsPorDia - DesfaseColombiaMs;
}
