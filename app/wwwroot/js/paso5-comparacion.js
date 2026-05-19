(function () {
    'use strict';

    const r = window.__resultado;
    if (!r) return;

    const fdm = r.metricasFDM || {};
    const fem = r.metricasFEM || {};
    const cmp = r.comparacion || {};

    renderTabla();
    renderVeredicto();
    renderBarras();

    const btnCsv = document.getElementById('btn-export-csv');
    const btnJson = document.getElementById('btn-export-json');
    if (btnCsv) btnCsv.addEventListener('click', exportarCSV);
    if (btnJson) btnJson.addEventListener('click', exportarJSON);

    function renderTabla() {
        const tbody = document.getElementById('cmp-tabla');
        if (!tbody) return;

        const filas = [
            { metrica: 'Tiempo (s)',  fdmV: fdm.tiempoSegundos, femV: fem.tiempoSegundos, ganador: cmp.ganadorTiempo, mejor: 'menor' },
            { metrica: 'Residuo',     fdmV: fdm.residuo,        femV: fem.residuo,        ganador: menorEs(fdm.residuo, fem.residuo), mejor: 'menor' },
            { metrica: 'Error',       fdmV: fdm.error,          femV: fem.error,          ganador: cmp.ganadorError, mejor: 'menor' },
            { metrica: 'Iteraciones', fdmV: fdm.numIteraciones, femV: fem.numIteraciones, ganador: menorEs(fdm.numIteraciones, fem.numIteraciones), mejor: 'menor' },
            { metrica: 'Tamaño malla',fdmV: fdm.tamanoMalla,    femV: fem.tamanoMalla,    ganador: '—', mejor: '' }
        ];

        tbody.innerHTML = filas.map(f => {
            const winFdm = f.ganador === 'FDM' ? 'win-fdm' : '';
            const winFem = f.ganador === 'FEM' ? 'win-fem' : '';
            return `<tr>
                <td>${escapar(f.metrica)}</td>
                <td class="text-end ${winFdm}">${fmt(f.fdmV)}</td>
                <td class="text-end ${winFem}">${fmt(f.femV)}</td>
                <td class="text-center"><strong>${escapar(f.ganador || '—')}</strong></td>
            </tr>`;
        }).join('');
    }

    function renderVeredicto() {
        const v = document.getElementById('veredicto-texto');
        if (!v) return;
        const partes = [];
        if (cmp.ganadorTiempo) partes.push(`${cmp.ganadorTiempo} es más rápido`);
        if (cmp.ganadorError) partes.push(`${cmp.ganadorError} tiene menor error`);
        v.textContent = partes.length ? partes.join(' · ') : 'Resultados disponibles.';
    }

    function renderBarras() {
        const div = document.getElementById('cmp-barras');
        if (!div || !window.Plotly) return;

        const data = [
            {
                type: 'bar', name: 'FDM', x: ['Tiempo (s)', 'Error', 'Residuo'],
                y: [fdm.tiempoSegundos || 0, fdm.error || 0, fdm.residuo || 0],
                marker: { color: '#C2410C' }
            },
            {
                type: 'bar', name: 'FEM', x: ['Tiempo (s)', 'Error', 'Residuo'],
                y: [fem.tiempoSegundos || 0, fem.error || 0, fem.residuo || 0],
                marker: { color: '#1E40AF' }
            }
        ];
        const layout = {
            barmode: 'group',
            margin: { l: 50, r: 10, t: 30, b: 40 },
            height: 280,
            yaxis: { type: 'log', title: 'valor (log)' },
            legend: { orientation: 'h' }
        };
        Plotly.newPlot(div, data, layout, { responsive: true, displaylogo: false });
    }

    function exportarCSV() {
        const filas = [
            ['metrica', 'FDM', 'FEM', 'ganador'],
            ['tiempo_s', fdm.tiempoSegundos, fem.tiempoSegundos, cmp.ganadorTiempo],
            ['residuo', fdm.residuo, fem.residuo, menorEs(fdm.residuo, fem.residuo)],
            ['error', fdm.error, fem.error, cmp.ganadorError],
            ['iteraciones', fdm.numIteraciones, fem.numIteraciones, menorEs(fdm.numIteraciones, fem.numIteraciones)],
            ['tamano_malla', fdm.tamanoMalla, fem.tamanoMalla, '']
        ];
        const csv = filas.map(row => row.map(c => `"${String(c ?? '').replace(/"/g, '""')}"`).join(',')).join('\n');
        descargar(csv, 'comparacion.csv', 'text/csv');
    }

    function exportarJSON() {
        const blob = JSON.stringify(r, null, 2);
        descargar(blob, 'resultado.json', 'application/json');
    }

    function descargar(contenido, nombre, mime) {
        const blob = new Blob([contenido], { type: mime + ';charset=utf-8' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = nombre;
        document.body.appendChild(a); a.click();
        document.body.removeChild(a);
        setTimeout(() => URL.revokeObjectURL(url), 500);
    }

    function menorEs(a, b) {
        if (a === null || a === undefined) return 'FEM';
        if (b === null || b === undefined) return 'FDM';
        return a <= b ? 'FDM' : 'FEM';
    }

    function fmt(x) {
        if (x === null || x === undefined || (typeof x === 'number' && isNaN(x))) return '—';
        if (typeof x !== 'number') return String(x);
        if (Math.abs(x) > 0 && (Math.abs(x) < 1e-3 || Math.abs(x) >= 1e6)) return x.toExponential(3);
        return Number(x).toFixed(4);
    }

    function escapar(s) {
        return String(s ?? '').replace(/[&<>"']/g, m => ({
            '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
        }[m]));
    }
})();
