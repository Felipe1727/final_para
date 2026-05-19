(function () {
    'use strict';

    const form = document.getElementById('form-config');
    if (!form) return;

    form.addEventListener('submit', async (ev) => {
        ev.preventDefault();
        const config = leerFormulario(form);
        const btn = document.getElementById('btn-resolver');
        if (btn) btn.disabled = true;

        console.log('Configuración enviada:', JSON.stringify(config, null, 2));

        try {
            const resp = await fetch('/Wizard/GuardarConfiguracion', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(config)
            });
            if (!resp.ok) {
                const err = await resp.json().catch(() => ({ error: 'Error al guardar configuración' }));
                const msg = err.detalles 
                    ? `${err.error}\n\nDetalles:\n${err.detalles.join('\n')}` 
                    : (err.error || `HTTP ${resp.status}`);
                throw new Error(msg);
            }
            const data = await resp.json();
            window.location.href = data.siguienteUrl || '/Wizard/Progreso';
        } catch (err) {
            const msg = err.message || 'Error desconocido';
            alert('Error: ' + msg);
            if (btn) btn.disabled = false;
        }
    });

    function leerFormulario(form) {
        const fd = new FormData(form);
        const obj = {};
        for (const [k, v] of fd.entries()) {
            const num = Number(v);
            obj[k] = (v !== '' && !isNaN(num) && /^-?\d/.test(v)) ? num : v;
        }
        return obj;
    }
})();
