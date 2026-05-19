(function () {
    'use strict';

    const inputTexto = document.getElementById('texto-ecuacion');
    const btnParsear = document.getElementById('btn-parsear');
    const resultado = document.getElementById('resultado-parseo');
    const latexBox = document.getElementById('latex-render');
    const metaBox = document.getElementById('meta-ecuacion');
    const erroresBox = document.getElementById('errores-parseo');
    const btnContinuar = document.getElementById('btn-continuar');
    const plantillasList = document.getElementById('plantillas-lista');

    if (!btnParsear) return;

    function escapar(s) {
        return String(s).replace(/[&<>"']/g, m => ({
            '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
        }[m]));
    }

    // Plantillas de ejemplo
    const plantillas = [
        { nombre: 'Calor 1D', texto: 'u_t = u_xx' }
    ];

    // Cargar plantillas
    if (plantillasList) {
        plantillasList.innerHTML = plantillas
            .map(p => `<button type="button" class="btn btn-sm btn-outline-primary template-btn" data-texto="${escapar(p.texto)}">${p.nombre}</button>`)
            .join('');

        document.querySelectorAll('.template-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.preventDefault();
                inputTexto.value = btn.dataset.texto;
                inputTexto.focus();
            });
        });
    }

    btnParsear.addEventListener('click', async () => {
        const texto = (inputTexto.value || '').trim();
        if (!texto) return;

        btnParsear.disabled = true;
        try {
            const resp = await fetch('/Ecuacion/Parsear', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ TextoPlano: texto })
            });
            const data = await resp.json();
            renderResultado(data);
        } catch (err) {
            mostrarError('Error de red: ' + err.message);
        } finally {
            btnParsear.disabled = false;
        }
    });

    function renderResultado(data) {
        resultado.classList.remove('d-none');

        // LaTeX
        latexBox.innerHTML = data.latex ? `\\(${data.latex}\\)` : '<em>(sin LaTeX)</em>';
        if (window.MathJax && window.MathJax.typesetPromise) {
            window.MathJax.typesetPromise([latexBox]);
        }

        // Variables / orden
        if (data.esValida) {
            metaBox.classList.remove('d-none');
            document.getElementById('meta-vars').textContent = (data.variablesIndependientes || []).join(', ');
            document.getElementById('meta-orden').textContent = data.orden;
        } else {
            metaBox.classList.add('d-none');
        }

        // Errores
        if (data.errores && data.errores.length > 0) {
            erroresBox.classList.remove('d-none');
            erroresBox.querySelector('ul').innerHTML = data.errores.map(e => `<li>${escapar(e)}</li>`).join('');
        } else {
            erroresBox.classList.add('d-none');
        }

        // Habilitar paso 2
        if (data.esValida) {
            btnContinuar.classList.remove('d-none');
        } else {
            btnContinuar.classList.add('d-none');
        }
    }

    function mostrarError(msg) {
        erroresBox.classList.remove('d-none');
        erroresBox.querySelector('ul').innerHTML = `<li>${escapar(msg)}</li>`;
    }

    if (btnContinuar) {
        btnContinuar.addEventListener('click', () => {
            btnContinuar.disabled = true;
            setTimeout(() => window.location.href = '/Wizard/Configuracion', 100);
        });
    }
})();
