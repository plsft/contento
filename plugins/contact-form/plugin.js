// name: "contact-form"
// version: "1.0.0"
// description: "Renders a styled contact form wherever a <div id='contento-contact-form'></div> placeholder appears in post content."
// author: "Contento"
// hooks: ["post:render"]

(function () {
    contento.on('post:render', function (context) {
        var doc = context.document;
        if (!doc) return;

        var body = doc.body || doc.getElementsByTagName('body')[0];
        var head = doc.head || doc.getElementsByTagName('head')[0];
        if (!body || !head) return;

        var placeholder = doc.getElementById('contento-contact-form');
        if (!placeholder) return;

        // --- Inject Styles ---
        var style = doc.createElement('style');
        style.textContent = ''
            + '.cf-form {'
            + '  max-width: 560px; margin: 2rem auto; padding: 2rem;'
            + '  background: var(--color-snow, #FFFFFF);'
            + '  border: 1px solid var(--color-mist, #E5E5E3);'
            + '  border-radius: 8px;'
            + '}'
            + '.cf-title {'
            + '  font-size: 1.25rem; font-weight: 600;'
            + '  color: var(--color-ink, #1A1A1A);'
            + '  margin-bottom: 0.25rem;'
            + '}'
            + '.cf-subtitle {'
            + '  font-size: 0.875rem; color: var(--color-ash, #9A9A9A);'
            + '  margin-bottom: 1.5rem;'
            + '}'
            + '.cf-field { margin-bottom: 1rem; }'
            + '.cf-label {'
            + '  display: block; font-size: 0.8125rem; font-weight: 500;'
            + '  color: var(--color-stone, #6B6B6B);'
            + '  margin-bottom: 0.375rem; letter-spacing: 0.02em;'
            + '}'
            + '.cf-label .cf-required { color: var(--color-danger, #DC2626); }'
            + '.cf-input, .cf-textarea {'
            + '  display: block; width: 100%; padding: 0.625rem 0.75rem;'
            + '  color: var(--color-ink, #1A1A1A);'
            + '  background: var(--color-snow, #FFFFFF);'
            + '  border: 1px solid var(--color-mist, #E5E5E3);'
            + '  border-radius: 0.375rem; font-size: 0.875rem;'
            + '  font-family: inherit; box-sizing: border-box;'
            + '  transition: border-color 150ms ease, box-shadow 150ms ease;'
            + '}'
            + '.cf-input:focus, .cf-textarea:focus {'
            + '  border-color: var(--color-indigo, #3D5A80);'
            + '  box-shadow: 0 0 0 2px rgba(61, 90, 128, 0.15);'
            + '  outline: none;'
            + '}'
            + '.cf-textarea { resize: vertical; min-height: 120px; }'
            + '.cf-error-text {'
            + '  font-size: 0.75rem; color: var(--color-danger, #DC2626);'
            + '  margin-top: 0.25rem; display: none;'
            + '}'
            + '.cf-error-text.visible { display: block; }'
            + '.cf-input.invalid, .cf-textarea.invalid {'
            + '  border-color: var(--color-danger, #DC2626);'
            + '}'
            + '.cf-submit {'
            + '  display: inline-flex; align-items: center; justify-content: center;'
            + '  gap: 0.5rem; padding: 0.625rem 1.25rem;'
            + '  background: var(--color-indigo, #3D5A80);'
            + '  color: var(--color-snow, #FFFFFF);'
            + '  font-weight: 500; font-size: 0.875rem;'
            + '  border: none; border-radius: 0.375rem; cursor: pointer;'
            + '  transition: background 150ms ease;'
            + '}'
            + '.cf-submit:hover { background: #34506F; }'
            + '.cf-submit:disabled { opacity: 0.5; cursor: not-allowed; }'
            + '.cf-honeypot { position: absolute; left: -9999px; opacity: 0; height: 0; }'
            + '.cf-message {'
            + '  padding: 1rem; border-radius: 0.375rem; margin-top: 1rem;'
            + '  font-size: 0.875rem; display: none;'
            + '}'
            + '.cf-message.cf-success {'
            + '  display: block; background: #ECFDF5; color: #065F46; border: 1px solid #A7F3D0;'
            + '  animation: cfFadeIn 0.3s ease;'
            + '}'
            + '.cf-message.cf-fail {'
            + '  display: block; background: #FEF2F2; color: #991B1B; border: 1px solid #FECACA;'
            + '  animation: cfFadeIn 0.3s ease;'
            + '}'
            + '@keyframes cfFadeIn { from { opacity: 0; transform: translateY(-4px); } to { opacity: 1; transform: translateY(0); } }';
        head.appendChild(style);

        // --- Render Form ---
        placeholder.innerHTML = ''
            + '<div class="cf-form">'
            + '  <div class="cf-title">Get in Touch</div>'
            + '  <div class="cf-subtitle">Fill out the form below and we\'ll get back to you.</div>'
            + '  <form id="cf-contact-form" novalidate>'
            + '    <div class="cf-field">'
            + '      <label class="cf-label">Name <span class="cf-required">*</span></label>'
            + '      <input type="text" name="cf-name" class="cf-input" required autocomplete="name" />'
            + '      <div class="cf-error-text" data-for="cf-name">Please enter your name.</div>'
            + '    </div>'
            + '    <div class="cf-field">'
            + '      <label class="cf-label">Email <span class="cf-required">*</span></label>'
            + '      <input type="email" name="cf-email" class="cf-input" required autocomplete="email" />'
            + '      <div class="cf-error-text" data-for="cf-email">Please enter a valid email address.</div>'
            + '    </div>'
            + '    <div class="cf-field">'
            + '      <label class="cf-label">Subject</label>'
            + '      <input type="text" name="cf-subject" class="cf-input" autocomplete="off" />'
            + '    </div>'
            + '    <div class="cf-field">'
            + '      <label class="cf-label">Message <span class="cf-required">*</span></label>'
            + '      <textarea name="cf-message" class="cf-textarea" required></textarea>'
            + '      <div class="cf-error-text" data-for="cf-message">Please enter a message (at least 10 characters).</div>'
            + '    </div>'
            + '    <div class="cf-honeypot">'
            + '      <label>Leave this empty</label>'
            + '      <input type="text" name="cf-website" tabindex="-1" autocomplete="off" />'
            + '    </div>'
            + '    <button type="submit" class="cf-submit">Send Message</button>'
            + '    <div id="cf-result" class="cf-message"></div>'
            + '  </form>'
            + '</div>';

        // --- Validation & Submission ---
        var form = doc.getElementById('cf-contact-form');
        if (!form) return;

        function showError(name) {
            var input = form.querySelector('[name="' + name + '"]');
            var err = form.querySelector('[data-for="' + name + '"]');
            if (input) input.classList.add('invalid');
            if (err) err.classList.add('visible');
        }

        function clearErrors() {
            var inputs = form.querySelectorAll('.invalid');
            for (var i = 0; i < inputs.length; i++) inputs[i].classList.remove('invalid');
            var errs = form.querySelectorAll('.cf-error-text.visible');
            for (var j = 0; j < errs.length; j++) errs[j].classList.remove('visible');
        }

        function isValidEmail(email) {
            return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
        }

        form.addEventListener('submit', function (e) {
            e.preventDefault();
            clearErrors();

            var honeypot = form.querySelector('[name="cf-website"]');
            if (honeypot && honeypot.value) {
                // Bot detected — silently show success to not alert the bot
                var result = doc.getElementById('cf-result');
                if (result) {
                    result.className = 'cf-message cf-success';
                    result.textContent = 'Thank you! Your message has been sent.';
                }
                return;
            }

            var name = (form.querySelector('[name="cf-name"]').value || '').trim();
            var email = (form.querySelector('[name="cf-email"]').value || '').trim();
            var message = (form.querySelector('[name="cf-message"]').value || '').trim();
            var valid = true;

            if (!name) { showError('cf-name'); valid = false; }
            if (!email || !isValidEmail(email)) { showError('cf-email'); valid = false; }
            if (!message || message.length < 10) { showError('cf-message'); valid = false; }

            if (!valid) return;

            // Disable submit while "sending"
            var submitBtn = form.querySelector('.cf-submit');
            if (submitBtn) { submitBtn.disabled = true; submitBtn.textContent = 'Sending...'; }

            // Simulate form submission (in a real deployment this would POST to an API)
            setTimeout(function () {
                var result = doc.getElementById('cf-result');
                if (result) {
                    result.className = 'cf-message cf-success';
                    result.textContent = 'Thank you, ' + name + '! Your message has been sent. We\'ll get back to you at ' + email + '.';
                }
                form.reset();
                if (submitBtn) { submitBtn.disabled = false; submitBtn.textContent = 'Send Message'; }
            }, 800);
        });

        // Clear field errors on input
        var allInputs = form.querySelectorAll('.cf-input, .cf-textarea');
        for (var k = 0; k < allInputs.length; k++) {
            allInputs[k].addEventListener('input', function () {
                this.classList.remove('invalid');
                var errEl = form.querySelector('[data-for="' + this.name + '"]');
                if (errEl) errEl.classList.remove('visible');
            });
        }
    });
})();
