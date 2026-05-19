(function () {
    'use strict';

    const r = window.__resultado;
    if (!r) return;

    const ejeX = r.ejeX || [];
    const ejeY = r.ejeY || [];           // tiempo
    const mallaFDM = r.mallaFDM || [];
    const mallaFEM = r.mallaFEM || [];
    const evolucionFDM = r.evolucionFDM || [];
    const evolucionFEM = r.evolucionFEM || [];

    const slider = document.getElementById('slider-tiempo');
    const sliderValor = document.getElementById('slider-tiempo-valor');

    const nT = Math.max(mallaFDM.length, mallaFEM.length, 1);
    slider.min = '0';
    slider.max = String(Math.max(0, nT - 1));
    slider.value = '0';
    sliderValor.textContent = formatearTiempo(0);

    dibujarSuperficie('fdm-surface', mallaFDM, ejeX, ejeY, 'FDM', '#C2410C');
    dibujarSuperficie('fem-surface', mallaFEM, ejeX, ejeY, 'FEM', '#1E40AF');

    dibujarHeatmap('fdm-heatmap', mallaFDM, ejeX, ejeY, 'FDM');
    dibujarHeatmap('fem-heatmap', mallaFEM, ejeX, ejeY, 'FEM');

    dibujarCorte('fdm-corte', mallaFDM, ejeX, 0, '#C2410C', 'FDM');
    dibujarCorte('fem-corte', mallaFEM, ejeX, 0, '#1E40AF', 'FEM');

    dibujarEvolucion('fdm-evolucion', evolucionFDM, '#C2410C');
    dibujarEvolucion('fem-evolucion', evolucionFEM, '#1E40AF');

    slider.addEventListener('input', () => {
        const idx = Number(slider.value);
        sliderValor.textContent = formatearTiempo(idx);
        actualizarCortes(idx);
    });

    sincronizarCamaras('fdm-surface', 'fem-surface');

    function formatearTiempo(idx) {
        const t = (ejeY[idx] !== undefined) ? ejeY[idx] : idx;
        return Number(t).toFixed(3);
    }

    function dibujarSuperficie(id, malla, x, y, titulo, color) {
        const div = document.getElementById(id);
        if (!div || !malla || malla.length === 0) {
            if (div) div.innerHTML = placeholder('Sin datos');
            return;
        }
        const data = [{
            type: 'surface',
            z: malla,
            x: x && x.length > 0 ? x : undefined,
            y: y && y.length > 0 ? y : undefined,
            showscale: false,
            colorscale: 'Viridis'
        }];
        const layout = {
            title: { text: titulo, font: { size: 13 } },
            margin: { l: 0, r: 0, t: 30, b: 0 },
            height: 320,
            scene: { xaxis: { title: 'x' }, yaxis: { title: 't' }, zaxis: { title: 'u' } }
        };
        Plotly.newPlot(div, data, layout, { responsive: true, displaylogo: false });
    }

    function dibujarHeatmap(id, malla, x, y, titulo) {
        const div = document.getElementById(id);
        if (!div || !malla || malla.length === 0) {
            if (div) div.innerHTML = placeholder('Sin datos');
            return;
        }
        const data = [{
            type: 'heatmap',
            z: malla,
            x: x && x.length > 0 ? x : undefined,
            y: y && y.length > 0 ? y : undefined,
            colorscale: 'Viridis',
            showscale: true
        }];
        const layout = {
            title: { text: titulo + ' · heatmap', font: { size: 13 } },
            margin: { l: 40, r: 10, t: 30, b: 40 },
            height: 260,
            xaxis: { title: 'x' },
            yaxis: { title: 't' }
        };
        Plotly.newPlot(div, data, layout, { responsive: true, displaylogo: false });
    }

    function dibujarCorte(id, malla, x, idxT, color, etiqueta) {
        const div = document.getElementById(id);
        if (!div || !malla || malla.length === 0) {
            if (div) div.innerHTML = placeholder('Sin datos');
            return;
        }
        const idx = Math.min(idxT, malla.length - 1);
        const z = malla[idx] || [];
        const data = [{
            type: 'scatter',
            mode: 'lines',
            x: x && x.length > 0 ? x : z.map((_, i) => i),
            y: z,
            line: { color: color, width: 2 },
            name: etiqueta
        }];
        const layout = {
            title: { text: etiqueta + ' · u(x, t=' + (idxT) + ')', font: { size: 13 } },
            margin: { l: 40, r: 10, t: 30, b: 40 },
            height: 220,
            xaxis: { title: 'x' },
            yaxis: { title: 'u' }
        };
        Plotly.newPlot(div, data, layout, { responsive: true, displaylogo: false });
    }

    function actualizarCortes(idxT) {
        actualizarCorte('fdm-corte', mallaFDM, idxT);
        actualizarCorte('fem-corte', mallaFEM, idxT);
    }

    function actualizarCorte(id, malla, idxT) {
        const div = document.getElementById(id);
        if (!div || !malla || malla.length === 0) return;
        const idx = Math.min(idxT, malla.length - 1);
        const z = malla[idx] || [];
        Plotly.restyle(div, { y: [z] }, [0]);
        Plotly.relayout(div, { 'title.text': div.id.startsWith('fdm') ? 'FDM' : 'FEM' + ' · u(x, t=' + idxT + ')' });
    }

    function sincronizarCamaras(idA, idB) {
        const a = document.getElementById(idA);
        const b = document.getElementById(idB);
        if (!a || !b) return;
        let sync = false;

        const handler = (origen, destino) => (evt) => {
            if (sync) return;
            if (!evt || !evt['scene.camera']) return;
            sync = true;
            try {
                Plotly.relayout(destino, { 'scene.camera': evt['scene.camera'] });
            } finally {
                setTimeout(() => { sync = false; }, 50);
            }
        };

        a.on('plotly_relayout', handler(a, b));
        b.on('plotly_relayout', handler(b, a));
    }

    function dibujarEvolucion(id, evolucion, color) {
        const div = document.getElementById(id);
        if (!div || !evolucion || evolucion.length === 0) {
            if (div) div.innerHTML = placeholder('Sin datos de evolución');
            return;
        }
        const tiempos = evolucion.map(e => e.tiempo);
        const residuos = evolucion.map(e => e.residuo);
        const data = [{
            type: 'scatter',
            mode: 'lines',
            x: tiempos,
            y: residuos,
            line: { color: color, width: 2 },
            name: 'Residuo'
        }];
        const layout = {
            title: { text: 'Evolución · Tiempo vs Residuo', font: { size: 13 } },
            margin: { l: 40, r: 10, t: 30, b: 40 },
            height: 260,
            xaxis: { title: 'Tiempo (s)', type: 'log' },
            yaxis: { title: 'Residuo', type: 'log' }
        };
        Plotly.newPlot(div, data, layout, { responsive: true, displaylogo: false });
    }

    function placeholder(msg) {
        return `<div class="placeholder p-4 text-center text-muted">${msg}</div>`;
    }
})();
