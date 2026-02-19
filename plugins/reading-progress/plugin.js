// name: "reading-progress"
// version: "1.0.0"
// description: "Displays a reading progress bar at the top of the page that tracks scroll position."
// author: "Contento"
// hooks: ["post:render"]

(function () {
    contento.on('post:render', function (context) {
        var doc = context.document;

        if (!doc) {
            return;
        }

        var body = doc.body || doc.getElementsByTagName('body')[0];
        var head = doc.head || doc.getElementsByTagName('head')[0];

        if (!body || !head) {
            return;
        }

        // Inject the progress bar element
        var progressBar = doc.createElement('div');
        progressBar.id = 'contento-reading-progress';
        progressBar.setAttribute('role', 'progressbar');
        progressBar.setAttribute('aria-valuemin', '0');
        progressBar.setAttribute('aria-valuemax', '100');
        progressBar.setAttribute('aria-valuenow', '0');
        progressBar.setAttribute('aria-label', 'Reading progress');

        // Insert as the first child of body so it sits at the very top
        if (body.firstChild) {
            body.insertBefore(progressBar, body.firstChild);
        } else {
            body.appendChild(progressBar);
        }

        // Inject styles
        var style = doc.createElement('style');
        style.textContent = ''
            + '#contento-reading-progress {'
            + '  position: fixed;'
            + '  top: 0;'
            + '  left: 0;'
            + '  width: 0%;'
            + '  height: 3px;'
            + '  background: linear-gradient(90deg, #3B82F6, #8B5CF6);'
            + '  z-index: 99999;'
            + '  transition: width 0.1s linear;'
            + '  pointer-events: none;'
            + '}';
        head.appendChild(style);

        // Throttle helper to limit scroll handler frequency
        var ticking = false;

        function updateProgress() {
            var scrollTop = window.pageYOffset || doc.documentElement.scrollTop || body.scrollTop || 0;
            var docHeight = Math.max(
                body.scrollHeight || 0,
                doc.documentElement.scrollHeight || 0,
                body.offsetHeight || 0,
                doc.documentElement.offsetHeight || 0
            );
            var winHeight = window.innerHeight || doc.documentElement.clientHeight || body.clientHeight || 0;
            var scrollable = docHeight - winHeight;

            var percent = 0;
            if (scrollable > 0) {
                percent = Math.min(Math.max((scrollTop / scrollable) * 100, 0), 100);
            }

            progressBar.style.width = percent + '%';
            progressBar.setAttribute('aria-valuenow', Math.round(percent).toString());
            ticking = false;
        }

        function onScroll() {
            if (!ticking) {
                ticking = true;
                if (window.requestAnimationFrame) {
                    window.requestAnimationFrame(updateProgress);
                } else {
                    setTimeout(updateProgress, 16);
                }
            }
        }

        window.addEventListener('scroll', onScroll, { passive: true });
        window.addEventListener('resize', onScroll, { passive: true });

        // Set initial state
        updateProgress();
    });
})();
