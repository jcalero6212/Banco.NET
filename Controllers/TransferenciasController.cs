using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Configuration;
using BancoApi.Models; 

namespace BancoApi.Controllers
{
    [ApiController]
[Route("api/[controller]")]
public class TransferenciasController : ControllerBase
{
    private readonly IConfiguration _config;

    public TransferenciasController(IConfiguration config)
    {
        _config = config;
    }

        [HttpGet]
        public IActionResult ObtenerTransferencias()
        {
            var lista = new List<Transferencia>();

            using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            con.Open();

            var cmd = new SqlCommand("SELECT NUM_TRA, FEC_TRA, VALOR_TRA, NUM_CUE_ORI, NUM_CUE_DES FROM TRANSFERENCIAS", con);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Transferencia
                {
                    NumTransaccion = Convert.ToInt32(reader["NUM_TRA"]),
                    Fecha = Convert.ToDateTime(reader["FEC_TRA"]),
                    Valor = Convert.ToDecimal(reader["VALOR_TRA"]),
                    CuentaOrigen = reader["NUM_CUE_ORI"].ToString(),
                    CuentaDestino = reader["NUM_CUE_DES"].ToString()
                });
            }

            return Ok(lista);
        }


        [HttpPost]
    public IActionResult Transferir([FromBody] Transferencia t)
    {
        using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        con.Open();

        using var tran = con.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            // Verifica cuenta origen y saldo
            var cmdSaldo = new SqlCommand("SELECT SAL_CUE FROM CUENTAS WHERE NUM_CUE = @ori", con, tran);
            cmdSaldo.Parameters.AddWithValue("@ori", t.CuentaOrigen);
            object result = cmdSaldo.ExecuteScalar();
            if (result == null) return BadRequest("Cuenta origen no existe");

            decimal saldo = Convert.ToDecimal(result);
            if (saldo < t.Valor)
                return BadRequest($"Saldo insuficiente. Disponible: {saldo}");

            // Verifica cuenta destino
            var cmdExiste = new SqlCommand("SELECT COUNT(*) FROM CUENTAS WHERE NUM_CUE = @des", con, tran);
            cmdExiste.Parameters.AddWithValue("@des", t.CuentaDestino);
            if ((int)cmdExiste.ExecuteScalar() == 0)
                return BadRequest("Cuenta destino no existe");

            // Descuento y crédito
            var debito = new SqlCommand("UPDATE CUENTAS SET SAL_CUE = SAL_CUE - @val WHERE NUM_CUE = @ori", con, tran);
            debito.Parameters.AddWithValue("@val", t.Valor);
            debito.Parameters.AddWithValue("@ori", t.CuentaOrigen);
            debito.ExecuteNonQuery();

            var credito = new SqlCommand("UPDATE CUENTAS SET SAL_CUE = SAL_CUE + @val WHERE NUM_CUE = @des", con, tran);
            credito.Parameters.AddWithValue("@val", t.Valor);
            credito.Parameters.AddWithValue("@des", t.CuentaDestino);
            credito.ExecuteNonQuery();

            // Registro de transferencia
            var insert = new SqlCommand(@"
                INSERT INTO TRANSFERENCIAS (FEC_TRA, VALOR_TRA, NUM_CUE_ORI, NUM_CUE_DES)
                VALUES (@fec, @val, @ori, @des)", con, tran);
            insert.Parameters.AddWithValue("@fec", t.Fecha);
            insert.Parameters.AddWithValue("@val", t.Valor);
            insert.Parameters.AddWithValue("@ori", t.CuentaOrigen);
            insert.Parameters.AddWithValue("@des", t.CuentaDestino);
            insert.ExecuteNonQuery();

            tran.Commit();
            return Ok("Transferencia realizada con éxito.");
        }
        catch (Exception ex)
        {
            tran.Rollback();
            return StatusCode(500, "Error: " + ex.Message);
        }
    }

        [HttpPut("{id}")]
        public IActionResult EditarTransferencia(int id, [FromBody] Transferencia t)
        {
            using var con = new SqlConnection(_config.GetConnectionString("cadenaSQL"));
            con.Open();

            var cmd = new SqlCommand(@"
        UPDATE TRANSFERENCIAS 
        SET FEC_TRA = @fecha, VALOR_TRA = @valor, 
            NUM_CUE_ORI = @origen, NUM_CUE_DES = @destino 
        WHERE NUM_TRA = @id", con);

            cmd.Parameters.AddWithValue("@fecha", t.Fecha);
            cmd.Parameters.AddWithValue("@valor", t.Valor);
            cmd.Parameters.AddWithValue("@origen", t.CuentaOrigen);
            cmd.Parameters.AddWithValue("@destino", t.CuentaDestino);
            cmd.Parameters.AddWithValue("@id", id);

            int filas = cmd.ExecuteNonQuery();
            return filas > 0 ? Ok("Transferencia actualizada correctamente") : NotFound("No se encontró la transferencia");
        }

        [HttpDelete("{id}")]
        public IActionResult EliminarTransferencia(int id)
        {
            using var con = new SqlConnection(_config.GetConnectionString("cadenaSQL"));
            con.Open();

            var cmd = new SqlCommand("DELETE FROM TRANSFERENCIAS WHERE NUM_TRA = @id", con);
            cmd.Parameters.AddWithValue("@id", id);

            int filas = cmd.ExecuteNonQuery();
            return filas > 0 ? Ok("Transferencia eliminada correctamente") : NotFound("No se encontró la transferencia");
        }

    }

}
