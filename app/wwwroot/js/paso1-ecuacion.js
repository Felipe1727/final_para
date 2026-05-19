(function () {
    'use strict';

    const inputTexto = document.getElementById('texto-ecuacion');
    const btnParsear = document.getElementById('btn-parsear');
    const resultado = document.getElementById('resultado-parseo');
    const latexBox = document.getElementById('latex-render');
    const metaBox = document.getElementById('meta-ecuacion');
    const erroresBox = document.getElementById('errores-parseo');
    const btnContinuar = document.getElementById('btn-continuar');
    const listaPlantillas = document.getElementById('plantillas-lista');

    if (!btnParsear) return;

    let siguienteUrl = null;

    cargarPlantillas();

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

    if (btnContinuar) {
        btnContinuar.addEventListener('click', () => {
            const url = siguienteUrl || btnContinuar.dataset.url || '/Wizard/Configuracion';
            window.location.href = url;
        });
    }

    inputTexto.addEventListener('keydown', (ev) => {
        if (ev.key === 'Enter') {
            ev.preventDefault();
            btnParsear.click();
        }
    });

    async function cargarPlantillas() {
        if (!listaPlantillas) return;
        try {
            const resp = await fetch('/Ecuacion/Plantillas');
            const plantillas = await resp.json();
            listaPlantillas.innerHTML = '';
            for (const p of plantillas) {
                const btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'btn btn-sm btn-outline-secondary';
                btn.textContent = p.nombre;
                btn.title = p.texto;
                btn.addEventListener('click', () => { inputTexto.value = p.texto; inputTexto.focus(); });
                listaPlantillas.appendChild(btn);
            }
        } catch (err) {
            listaPlantillas.innerHTML = '<span class="text-muted small">No se pudieron cargar plantillas.</span>';
        }
    }

    function renderResultado(data) {
        resultado.classList.remove('d-none');
        siguienteUrl = data.siguienteUrl || null;

        latexBox.innerHTML = data.latex ? `\\(${data.latex}\\)` : '<em>(sin LaTeX)</em>';
        if (window.finalPara && window.finalPara.renderizarLatex) {
            void window.finalPara.renderizarLatex(latexBox);
        } else if (window.MathJax && window.MathJax.typesetPromise) {
            window.MathJax.typesetPromise([latexBox]);
        }

        if (data.esValida) {
            metaBox.classList.remove('d-none');
            document.getElementById('meta-vars').textContent = (data.variablesIndependientes || []).join(', ');
            document.getElementById('meta-orden').textContent = data.orden;
        } else {
            metaBox.classList.add('d-none');
        }

        if (data.errores && data.errores.length > 0) {
            erroresBox.classList.remove('d-none');
            erroresBox.querySelector('ul').innerHTML = data.errores.map(e => `<li>${escapar(e)}</li>`).join('');
        } else {
            erroresBox.classList.add('d-none');
        }

        if (data.esValida && btnContinuar) {
            btnContinuar.classList.remove('d-none');
        } else if (btnContinuar) {
            btnContinuar.classList.add('d-none');
        }
    }

    function mostrarError(msg) {
        resultado.classList.remove('d-none');
        erroresBox.classList.remove('d-none');
        erroresBox.querySelector('ul').innerHTML = `<li>${escapar(msg)}</li>`;
    }

    function escapar(s) {
        return String(s).replace(/[&<>"']/g, m => ({
            '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
        }[m]));
    }
})();
