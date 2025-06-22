const API_URL = 'https://localhost:7117/api/Transferencias';
const form = document.getElementById('formTransferencia');
const tabla = document.getElementById('tablaTransferencias');
const btnCancelar = document.getElementById('btnCancelar');
let editandoId = null;

form.addEventListener('submit', async (e) => {
  e.preventDefault();

  const data = {
    cuentaOrigen: document.getElementById('origen').value.trim(),
    cuentaDestino: document.getElementById('destino').value.trim(),
    valor: parseFloat(document.getElementById('valor').value),
    fecha: document.getElementById('fecha').value
  };

  const url = editandoId ? `${API_URL}/${editandoId}` : API_URL;
  const method = editandoId ? 'PUT' : 'POST';

  try {
    const res = await fetch(url, {
      method,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });

    const texto = await res.text();
    if (!res.ok) throw new Error(texto);

    alert(editandoId ? 'Transferencia actualizada' : 'Transferencia registrada');
    cargarTransferencias();
    form.reset();
    editandoId = null;
    btnCancelar.style.display = 'none';
  } catch (error) {
    alert('Error: ' + error.message);
  }
});

btnCancelar.addEventListener('click', () => {
  form.reset();
  editandoId = null;
  btnCancelar.style.display = 'none';
});

async function cargarTransferencias() {
  tabla.innerHTML = '';
  try {
    const res = await fetch(API_URL);
    const datos = await res.json();

    datos.forEach(t => {
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${t.numTransaccion}</td>
        <td>${t.cuentaOrigen}</td>
        <td>${t.cuentaDestino}</td>
        <td>$${t.valor.toFixed(2)}</td>
        <td>${t.fecha.split('T')[0]}</td>
        <td>
          <button onclick="editar(${t.numTransaccion})">Editar</button>
          <button onclick="eliminar(${t.numTransaccion})">Eliminar</button>
        </td>`;
      tabla.appendChild(tr);
    });
  } catch (err) {
    tabla.innerHTML = '<tr><td colspan="6">Error al cargar</td></tr>';
  }
}

async function editar(id) {
  const res = await fetch(`${API_URL}/${id}`);
  if (!res.ok) return alert('No se pudo obtener la transferencia');

  const t = await res.json();
  document.getElementById('origen').value = t.cuentaOrigen;
  document.getElementById('destino').value = t.cuentaDestino;
  document.getElementById('valor').value = t.valor;
  document.getElementById('fecha').value = t.fecha.split('T')[0];

  editandoId = id;
  btnCancelar.style.display = 'inline-block';
}

async function eliminar(id) {
  if (!confirm('¿Eliminar transferencia?')) return;

  const res = await fetch(`${API_URL}/${id}`, { method: 'DELETE' });
  if (!res.ok) return alert('No se pudo eliminar');
  alert('Transferencia eliminada');
  cargarTransferencias();
}

cargarTransferencias();
