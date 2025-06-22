using System;
using System.Data.SqlClient;
using BancoApi.Models;

namespace BancoApi.Services
{
    public class BancoService
    {
        private readonly string cadena = "Server=TU_SERVIDOR;Database=TU_BD;Trusted_Connection=True;";

        public bool Transferir(Transferencia t, out string mensaje)
        {
            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();
                SqlTransaction tran = con.BeginTransaction();

                try
                {
                    // Verificar saldo
                    var cmdSaldo = new SqlCommand("SELECT SAL_CUE FROM CUENTAS WHERE NUM_CUE = @ori", con, tran);
                    cmdSaldo.Parameters.AddWithValue("@ori", t.CuentaOrigen);
                    object result = cmdSaldo.ExecuteScalar();

                    if (result == null)
                    {
                        mensaje = "La cuenta origen no existe.";
                        return false;
                    }

                    decimal saldo = Convert.ToDecimal(result);
                    if (saldo < t.Valor)
                    {
                        mensaje = $"Saldo insuficiente. Disponible: {saldo:C2}";
                        return false;
                    }

                    // Verificar que cuenta destino exista
                    var cmdDest = new SqlCommand("SELECT COUNT(*) FROM CUENTAS WHERE NUM_CUE = @des", con, tran);
                    cmdDest.Parameters.AddWithValue("@des", t.CuentaDestino);
                    if ((int)cmdDest.ExecuteScalar() == 0)
                    {
                        mensaje = "La cuenta destino no existe.";
                        return false;
                    }

                    // Transferencia: debito + credito + inserción
                    var cmdDebito = new SqlCommand("UPDATE CUENTAS SET SAL_CUE = SAL_CUE - @val WHERE NUM_CUE = @ori", con, tran);
                    cmdDebito.Parameters.AddWithValue("@val", t.Valor);
                    cmdDebito.Parameters.AddWithValue("@ori", t.CuentaOrigen);
                    cmdDebito.ExecuteNonQuery();

                    var cmdCredito = new SqlCommand("UPDATE CUENTAS SET SAL_CUE = SAL_CUE + @val WHERE NUM_CUE = @des", con, tran);
                    cmdCredito.Parameters.AddWithValue("@val", t.Valor);
                    cmdCredito.Parameters.AddWithValue("@des", t.CuentaDestino);
                    cmdCredito.ExecuteNonQuery();

                    var cmdInsert = new SqlCommand(@"
                        INSERT INTO TRANSFERENCIAS (FEC_TRA, VALOR_TRA, NUM_CUE_ORI, NUM_CUE_DES)
                        VALUES (@fec, @val, @ori, @des)", con, tran);
                    cmdInsert.Parameters.AddWithValue("@fec", t.Fecha);
                    cmdInsert.Parameters.AddWithValue("@val", t.Valor);
                    cmdInsert.Parameters.AddWithValue("@ori", t.CuentaOrigen);
                    cmdInsert.Parameters.AddWithValue("@des", t.CuentaDestino);
                    cmdInsert.ExecuteNonQuery();

                    tran.Commit();
                    mensaje = "Transferencia realizada exitosamente.";
                    return true;
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    mensaje = "Error al transferir: " + ex.Message;
                    return false;
                }
            }
        }
    }
}

