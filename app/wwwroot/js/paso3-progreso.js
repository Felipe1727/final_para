(function () {
    'use strict';

    const config = window.__config;
    if (!config) return;

    const checklist = document.getElementById('checklist-fases');
    const log = document.getElementById('progreso-log');
    const barra = document.getElementById('barra-progreso');
    const btnCancelar = document.getElementById('btn-cancelar');

    const iconos = { pendiente: '⚪', activo: '🔵', hecho: '✓', error: '✗' };

    const abortCtl = new AbortController();
    let conexion = null;
    let cancelado = false;

    const iterPorMetodo = {};

    arrancar();

    if (btnCancelar) {
        btnCancelar.addEventListener('click', async () => {
            cancelado = true;
            btnCancelar.disabled = true;
            try { abortCtl.abort(); } catch (e) { /* ignore */ }
            try { if (conexion) await conexion.stop(); } catch (e) { /* ignore */ }
            appendLog('— cancelado por el usuario —', 'text-warning');
        });
    }

    async function arrancar() {
        let sessionId;
        try {
            sessionId = await fetch('/Resolucion/SessionId').then(r => r.text());
        } catch (err) {
            appendLog('No se pudo obtener sessionId: ' + err.message, 'text-danger');
            return;
        }

        conexion = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/progreso')
            .build();

        conexion.on('FaseActualizada', (e) => actualizarFase(e));
        conexion.on('ProgresoActualizado', (e) => actualizarProgreso(e));
        conexion.on('ResolucionCompleta', () => {
            window.location.href = '/Wizard/Resultados';
        });

        try {
            await conexion.start();
            await conexion.invoke('Suscribir', sessionId);
        } catch (err) {
            appendLog('SignalR no disponible: ' + err.message, 'text-warning');
        }

        try {
            const resp = await fetch('/Resolucion/Iniciar', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(config),
                signal: abortCtl.signal
            });
            if (!resp.ok) {
                const err = await resp.json().catch(() => ({ error: 'Error de servidor' }));
                throw new Error(err.error || `HTTP ${resp.status}`);
            }
            // Asegura redirección incluso si SignalR no entregó ResolucionCompleta.
            await resp.json();
            window.location.href = '/Wizard/Resultados';
        } catch (err) {
            if (cancelado) return;
            appendLog('Error: ' + err.message, 'text-danger');
        } finally {
            try { if (conexion) await conexion.stop(); } catch (e) { /* ignore */ }
        }
    }

    function actualizarFase(e) {
        if (!e || !e.fase) return;
        const item = checklist.querySelector(`[data-fase="${e.fase}"]`);
        if (item) {
            item.classList.remove('pendiente', 'activo', 'hecho');
            if (e.estado === 'iniciado') {
                item.classList.add('activo');
                marcarIcono(item, iconos.activo);
            } else if (e.estado === 'completado') {
                item.classList.add('hecho');
                marcarIcono(item, iconos.hecho);
            }
        }
        if (typeof e.porcentaje === 'number' && barra) {
            barra.style.width = Math.max(0, Math.min(100, e.porcentaje)) + '%';
        }
        appendLog(`▸ ${e.fase} · ${e.estado}`);
    }

    function actualizarProgreso(e) {
        if (!e || !e.metodo) return;
        iterPorMetodo[e.metodo] = e.iteracion;
        const faseId = e.metodo.toUpperCase().includes('FDM') ? 'ResolviendoFDM' : 'ResolviendoFEM';
        const item = checklist.querySelector(`[data-fase="${faseId}"] .fase-iter`);
        if (item) {
            item.textContent = `(iter ${e.iteracion} · residuo ${Number(e.residuo).toExponential(2)})`;
        }
        appendLog(`${e.metodo} · iter=${e.iteracion} · residuo=${Number(e.residuo).toExponential(3)} · t=${Number(e.tiempo).toFixed(3)}s`);
    }

    function marcarIcono(item, icono) {
        // Si el primer carácter es un emoji círculo lo reemplaza; si tiene chip, se inserta antes.
        const txt = item.firstChild && item.firstChild.nodeType === Node.TEXT_NODE ? item.firstChild.nodeValue : '';
        if (/^[⚪🔵✓✗]/.test(txt.trim())) {
            item.firstChild.nodeValue = icono + ' ';
        } else {
            // Insertar antes del primer hijo
            const span = document.createElement('span');
            span.textContent = icono + ' ';
            item.insertBefore(span, item.firstChild);
        }
    }

    function appendLog(msg, cls) {
        if (!log) return;
        const fila = document.createElement('div');
        if (cls) fila.className = cls;
        fila.textContent = msg;
        log.appendChild(fila);
        log.scrollTop = log.scrollHeight;
    }
})();
