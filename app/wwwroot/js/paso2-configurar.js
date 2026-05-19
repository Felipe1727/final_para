(function () {
    'use strict';

    const form = document.getElementById('form-config');
    if (!form) return;

    form.addEventListener('submit', async (ev) => {
        ev.preventDefault();
        const config = leerFormulario(form);
        const btn = document.getElementById('btn-resolver');
        btn.disabled = true;

        try {
            const resp = await fetch('/Wizard/GuardarConfiguracion', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(config)
            });
            if (!resp.ok) {
                const data = await resp.json().catch(() => ({ error: 'Error de servidor' }));
                let mensaje = data.error || `Error HTTP ${resp.status}`;
                if (data.detalles && Array.isArray(data.detalles)) {
                    mensaje += '\n' + data.detalles.join('\n');
                }
                throw new Error(mensaje);
            }
            const data = await resp.json();
            if (data.siguienteUrl) {
                window.location.href = data.siguienteUrl;
            }
        } catch (err) {
            alert('Error al guardar configuración:\n' + err.message);
            btn.disabled = false;
        }
    });

    function leerFormulario(form) {
        const fd = new FormData(form);
        const obj = {};
        const numericFields = new Set(['XMin', 'XMax', 'TMin', 'TMax', 'Nx', 'Nt']);

        for (const [k, v] of fd.entries()) {
            if (numericFields.has(k)) {
                const num = Number(v);
                obj[k] = !isNaN(num) ? num : v;
            } else {
                obj[k] = v;
            }
        }
        return obj;
    }
})();
