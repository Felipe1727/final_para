// Render centralizado de LaTeX para el wizard.
(function () {
    'use strict';

    function dormir(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    function normalizarElementos(objetivo) {
        if (!objetivo) return [];
        if (typeof objetivo === 'string') return Array.from(document.querySelectorAll(objetivo));
        if (objetivo instanceof Element) return [objetivo];
        if (objetivo instanceof NodeList || Array.isArray(objetivo)) return Array.from(objetivo).filter(e => e instanceof Element);
        return [];
    }

    async function esperarMathJax(maxIntentos = 50, esperaMs = 50) {
        for (let i = 0; i < maxIntentos; i++) {
            const mj = window.MathJax;
            if (mj && typeof mj.typesetPromise === 'function') {
                if (mj.startup && mj.startup.promise) {
                    await mj.startup.promise;
                }
                return mj;
            }
            await dormir(esperaMs);
        }
        return null;
    }

    async function renderizarLatex(objetivo = '#ecuacion-latex, #latex-render') {
        const elementos = normalizarElementos(objetivo).filter(e => e.isConnected);
        if (!elementos.length) return false;

        const mj = await esperarMathJax();
        if (!mj) return false;

        await mj.typesetPromise(elementos);
        return true;
    }

    window.finalPara = window.finalPara || {};
    window.finalPara.renderizarLatex = renderizarLatex;

    document.addEventListener('DOMContentLoaded', () => {
        void renderizarLatex();
    });
})();
