(function () {
    'use strict';

    const form = document.getElementById('form-config');
    if (!form) return;

    form.addEventListener('submit', async (ev) => {
        ev.preventDefault();
        const config = leerFormulario(form);

        const paso3 = document.getElementById('paso-3');
        const panelProgreso = document.getElementById('panel-progreso');
        const panelResultados = document.getElementById('panel-resultados');
        const mensajesProg = document.getElementById('progreso-mensajes');

        paso3.classList.remove('d-none');
        panelProgreso.classList.remove('d-none');
        panelResultados.classList.add('d-none');
        mensajesProg.innerHTML = '';
        paso3.scrollIntoView({ behavior: 'smooth' });

        // Conectar SignalR antes de iniciar la resolución
        const sessionId = await fetch('/Resolucion/SessionId').then(r => r.text());
        const conexion = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/progreso')
            .build();

        conexion.on('ProgresoActualizado', (data) => {
            const fila = document.createElement('div');
            fila.textContent = `${data.metodo} · iter=${data.iteracion} · residuo=${Number(data.residuo).toExponential(3)} · t=${Number(data.tiempo).toFixed(3)}s`;
            mensajesProg.appendChild(fila);
            mensajesProg.scrollTop = mensajesProg.scrollHeight;
        });

        try {
            await conexion.start();
            await conexion.invoke('Suscribir', sessionId);
        } catch (err) {
            console.warn('SignalR no se pudo iniciar:', err);
        }

        const btn = document.getElementById('btn-resolver');
        btn.disabled = true;

        try {
            const resp = await fetch('/Resolucion/Iniciar', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(config)
            });
            if (!resp.ok) {
                const err = await resp.json().catch(() => ({ error: 'Error de servidor' }));
                throw new Error(err.error || `HTTP ${resp.status}`);
            }
            const resultado = await resp.json();
            window.__resultadoActual = resultado;
            window.dispatchEvent(new CustomEvent('resolucion:completa', { detail: resultado }));
        } catch (err) {
            const fila = document.createElement('div');
            fila.className = 'text-danger';
            fila.textContent = 'Error: ' + err.message;
            mensajesProg.appendChild(fila);
        } finally {
            btn.disabled = false;
            try { await conexion.stop(); } catch (e) { /* ignore */ }
        }
    });

    function leerFormulario(form) {
        const fd = new FormData(form);
        const obj = {};
        for (const [k, v] of fd.entries()) {
            // Convertir numéricos detectables
            const num = Number(v);
            obj[k] = (v !== '' && !isNaN(num) && /^-?\d/.test(v)) ? num : v;
        }
        return obj;
    }
})();
