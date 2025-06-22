namespace BancoApi.Models
{
    public class Transferencia
    {
        public int NumTransaccion { get; set; }
        public string CuentaOrigen { get; set; } = string.Empty;
        public string CuentaDestino { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public DateTime Fecha { get; set; }
    }

}
