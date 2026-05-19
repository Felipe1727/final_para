window.finalPara = window.finalPara || {};

window.finalPara.renderizarLatex = function(selector) {
    const renderizar = () => {
        if (typeof MathJax !== 'undefined' && MathJax.typesetPromise) {
            const elements = document.querySelectorAll(selector);
            if (elements.length > 0) {
                MathJax.typesetPromise(Array.from(elements))
                    .catch(err => console.error('Error renderizando LaTeX:', err));
            }
        }
    };

    if (typeof MathJax !== 'undefined' && MathJax.startup && MathJax.startup.promise) {
        MathJax.startup.promise.then(renderizar);
    } else {
        setTimeout(renderizar, 100);
    }
};
