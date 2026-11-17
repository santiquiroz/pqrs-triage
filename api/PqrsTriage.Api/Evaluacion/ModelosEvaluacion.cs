using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Api.Evaluacion;

public sealed record Obtenido(
    string? Tipo,
    string? Area,
    string? Motivo,
    string Prioridad,
    bool EsTutela,
    string? Activo,
    string Destino,
    double? ConfianzaTipologia);

public sealed record Aciertos(bool? Tipo, bool? Area, bool? Motivo, bool? Tutela, bool? Activo, bool? Destino);

public sealed record FilaEvaluacion(
    string CasoId,
    string Conjunto,
    Esperado? Esperado,
    Obtenido? Obtenido,
    Aciertos? Aciertos,
    long LatenciaMs,
    double CostoUsd,
    string? Error);

public sealed record ResumenTutela(int Vp, int Fn, int Fp, double? Recall, double? Precision);

public sealed record ResumenEvaluacion(
    string Proveedor,
    int Total,
    int Errores,
    double? ExactitudTipo,
    double? ExactitudArea,
    double? ExactitudTipologia,
    double? ExactitudActivo,
    double? ExactitudEnAutomaticos,
    ResumenTutela Tutela,
    double PorcentajeAutomatico,
    double PorcentajeGestor,
    double PorcentajeTutela,
    long LatenciaP50Ms,
    long LatenciaP95Ms,
    double CostoUsdPorMil);
