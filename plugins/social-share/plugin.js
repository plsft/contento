// name: "social-share"
// version: "1.0.0"
// description: "Injects floating social share buttons for Twitter/X, LinkedIn, and Copy Link on rendered posts."
// author: "Contento"
// hooks: ["post:render"]

(function () {
    contento.on('post:render', function (context) {
        var post = context.post;
        var doc = context.document;

        if (!post || !doc) {
            return;
        }

        var body = doc.body || doc.getElementsByTagName('body')[0];
        if (!body) {
            return;
        }

        var title = encodeURIComponent(post.title || '');
        var url = encodeURIComponent(post.url || '');

        var twitterUrl = 'https://x.com/intent/tweet?text=' + title + '&url=' + url;
        var linkedInUrl = 'https://www.linkedin.com/sharing/share-offsite/?url=' + url;

        // Build the share widget HTML
        var html = ''
            + '<div id="contento-social-share" style="'
            + 'position:fixed;right:20px;top:50%;transform:translateY(-50%);'
            + 'z-index:9999;display:flex;flex-direction:column;gap:8px;'
            + '">'

            // Twitter/X button
            + '<a href="' + twitterUrl + '" target="_blank" rel="noopener noreferrer" '
            + 'title="Share on X" '
            + 'style="'
            + 'display:flex;align-items:center;justify-content:center;'
            + 'width:44px;height:44px;border-radius:50%;'
            + 'background:#000;color:#fff;text-decoration:none;'
            + 'font-size:18px;font-weight:bold;'
            + 'box-shadow:0 2px 8px rgba(0,0,0,0.15);'
            + 'transition:transform 0.15s ease,box-shadow 0.15s ease;'
            + '" '
            + 'onmouseover="this.style.transform=\'scale(1.1)\';this.style.boxShadow=\'0 4px 12px rgba(0,0,0,0.25)\';" '
            + 'onmouseout="this.style.transform=\'scale(1)\';this.style.boxShadow=\'0 2px 8px rgba(0,0,0,0.15)\';"'
            + '>'
            + '<svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">'
            + '<path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/>'
            + '</svg>'
            + '</a>'

            // LinkedIn button
            + '<a href="' + linkedInUrl + '" target="_blank" rel="noopener noreferrer" '
            + 'title="Share on LinkedIn" '
            + 'style="'
            + 'display:flex;align-items:center;justify-content:center;'
            + 'width:44px;height:44px;border-radius:50%;'
            + 'background:#0A66C2;color:#fff;text-decoration:none;'
            + 'font-size:18px;font-weight:bold;'
            + 'box-shadow:0 2px 8px rgba(0,0,0,0.15);'
            + 'transition:transform 0.15s ease,box-shadow 0.15s ease;'
            + '" '
            + 'onmouseover="this.style.transform=\'scale(1.1)\';this.style.boxShadow=\'0 4px 12px rgba(0,0,0,0.25)\';" '
            + 'onmouseout="this.style.transform=\'scale(1)\';this.style.boxShadow=\'0 2px 8px rgba(0,0,0,0.15)\';"'
            + '>'
            + '<svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">'
            + '<path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433a2.062 2.062 0 01-2.063-2.065 2.064 2.064 0 112.063 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z"/>'
            + '</svg>'
            + '</a>'

            // Copy Link button
            + '<button id="contento-copy-link" '
            + 'title="Copy link" '
            + 'style="'
            + 'display:flex;align-items:center;justify-content:center;'
            + 'width:44px;height:44px;border-radius:50%;border:none;cursor:pointer;'
            + 'background:#6B7280;color:#fff;'
            + 'font-size:18px;font-weight:bold;'
            + 'box-shadow:0 2px 8px rgba(0,0,0,0.15);'
            + 'transition:transform 0.15s ease,box-shadow 0.15s ease,background 0.15s ease;'
            + '" '
            + 'onmouseover="this.style.transform=\'scale(1.1)\';this.style.boxShadow=\'0 4px 12px rgba(0,0,0,0.25)\';" '
            + 'onmouseout="this.style.transform=\'scale(1)\';this.style.boxShadow=\'0 2px 8px rgba(0,0,0,0.15)\';"'
            + '>'
            + '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">'
            + '<path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/>'
            + '<path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/>'
            + '</svg>'
            + '</button>'

            + '</div>';

        // Inject the widget
        var container = doc.createElement('div');
        container.innerHTML = html;
        body.appendChild(container);

        // Copy-to-clipboard handler
        var copyBtn = doc.getElementById('contento-copy-link');
        if (copyBtn) {
            copyBtn.addEventListener('click', function () {
                var pageUrl = post.url || window.location.href;
                if (navigator.clipboard && navigator.clipboard.writeText) {
                    navigator.clipboard.writeText(pageUrl).then(function () {
                        showCopiedFeedback(copyBtn);
                    });
                } else {
                    // Fallback for older browsers
                    var textarea = doc.createElement('textarea');
                    textarea.value = pageUrl;
                    textarea.style.position = 'fixed';
                    textarea.style.opacity = '0';
                    body.appendChild(textarea);
                    textarea.select();
                    doc.execCommand('copy');
                    body.removeChild(textarea);
                    showCopiedFeedback(copyBtn);
                }
            });
        }

        function showCopiedFeedback(btn) {
            var originalBg = btn.style.background;
            btn.style.background = '#10B981';
            btn.setAttribute('title', 'Copied!');
            setTimeout(function () {
                btn.style.background = originalBg;
                btn.setAttribute('title', 'Copy link');
            }, 2000);
        }
    });
})();
