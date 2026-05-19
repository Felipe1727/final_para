(function () {
    'use strict';

    window.addEventListener('resolucion:completa', (ev) => {
        const r = ev.detail;
        const panelProgreso = document.getElementById('panel-progreso');
        const panelResultados = document.getElementById('panel-resultados');

        panelProgreso.classList.add('d-none');
        panelResultados.classList.remove('d-none');

        const ejeX = r.ejeX || [];
        const ejeY = r.ejeY || [];

        dibujarSurface('grafico-fdm', r.mallaFDM, ejeX, ejeY, r.metricasFDM.metodoNombre || 'FDM');
        dibujarSurface('grafico-fem', r.mallaFEM, [], [], r.metricasFEM.metodoNombre || 'FEM');

        renderMetricas('metricas-fdm', r.metricasFDM);
        renderMetricas('metricas-fem', r.metricasFEM);

        renderComparacion('panel-comparacion', r.comparacion, r.metricasFDM, r.metricasFEM);
    });

    function dibujarSurface(divId, malla, ejeX, ejeY, titulo) {
        const div = document.getElementById(divId);
        if (!div || !malla || malla.length === 0) {
            if (div) div.innerHTML = '<div class="text-muted text-center py-5">Sin datos para graficar.</div>';
            return;
        }
        const data = [{
            type: 'surface',
            z: malla,
            x: ejeX && ejeX.length > 0 ? ejeX : undefined,
            y: ejeY && ejeY.length > 0 ? ejeY : undefined,
            showscale: true,
            colorscale: 'Viridis'
        }];
        const layout = {
            title: titulo,
            margin: { l: 0, r: 0, t: 30, b: 0 },
            scene: {
                xaxis: { title: 'x' },
                yaxis: { title: 't' },
                zaxis: { title: 'u' }
            }
        };
        Plotly.newPlot(div, data, layout, { responsive: true });
    }

    function renderMetricas(tbodyId, m) {
        const tbody = document.getElementById(tbodyId);
        if (!tbody) return;
        tbody.innerHTML = `
            <tr><th>Método</th><td>${escapar(m.metodoNombre)}</td></tr>
            <tr><th>Tiempo (s)</th><td>${num(m.tiempoSegundos, 4)}</td></tr>
            <tr><th>Residuo</th><td>${num(m.residuo, 6, true)}</td></tr>
            <tr><th>Error</th><td>${num(m.error, 6, true)}</td></tr>
            <tr><th>Iteraciones</th><td>${m.numIteraciones}</td></tr>
            <tr><th>Tamaño malla</th><td>${m.tamanoMalla}</td></tr>
        `;
    }

    function renderComparacion(divId, comp, fdm, fem) {
        const div = document.getElementById(divId);
        if (!div) return;
        div.innerHTML = `
            <h6>Comparación</h6>
            <table class="table table-sm table-bordered">
                <thead><tr><th>Métrica</th><th>FDM</th><th>FEM</th><th>Ganador</th></tr></thead>
                <tbody>
                    <tr><td>Tiempo (s)</td><td>${num(fdm.tiempoSegundos, 4)}</td><td>${num(fem.tiempoSegundos, 4)}</td><td><strong>${comp.ganadorTiempo}</strong></td></tr>
                    <tr><td>Error</td><td>${num(fdm.error, 6, true)}</td><td>${num(fem.error, 6, true)}</td><td><strong>${comp.ganadorError}</strong></td></tr>
                </tbody>
            </table>
        `;
    }

    function num(x, dec, exp) {
        if (x === null || x === undefined || isNaN(x)) return '-';
        if (exp) return Number(x).toExponential(dec);
        return Number(x).toFixed(dec);
    }

    function escapar(s) {
        return String(s ?? '').replace(/[&<>"']/g, m => ({
            '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
        }[m]));
    }
})();
