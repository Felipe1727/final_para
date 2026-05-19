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
                const err = await resp.json().catch(() => ({ error: 'Error de servidor' }));
                throw new Error(err.error || `HTTP ${resp.status}`);
            }
            const data = await resp.json();
            if (data.siguienteUrl) {
                window.location.href = data.siguienteUrl;
            }
        } catch (err) {
            alert('Error al guardar configuración: ' + err.message);
            btn.disabled = false;
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
