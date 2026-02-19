// name: "seo-analyzer"
// version: "1.0.0"
// description: "Analyzes posts for SEO quality and readability, showing a floating score widget with actionable feedback."
// author: "Contento"
// hooks: ["post:render"]

(function () {
    contento.on('post:render', function (context) {
        var doc = context.document;
        if (!doc) return;

        var body = doc.body || doc.getElementsByTagName('body')[0];
        var head = doc.head || doc.getElementsByTagName('head')[0];
        if (!body || !head) return;

        // Extract post data from context
        var title = context.title || '';
        var slug = context.slug || '';
        var metaTitle = context.metaTitle || title;
        var metaDescription = context.metaDescription || '';
        var content = context.content || '';
        var tags = context.tags || [];

        // --- SEO Scoring ---
        var seoChecks = [];
        var seoScore = 0;
        var seoMax = 0;

        // Title length (50-60 chars ideal)
        seoMax += 10;
        if (metaTitle.length >= 50 && metaTitle.length <= 60) {
            seoScore += 10;
            seoChecks.push({ pass: true, msg: 'Title length is ideal (' + metaTitle.length + ' chars)' });
        } else if (metaTitle.length >= 30 && metaTitle.length <= 70) {
            seoScore += 6;
            seoChecks.push({ pass: false, msg: 'Title length (' + metaTitle.length + ') — aim for 50-60 chars' });
        } else {
            seoChecks.push({ pass: false, msg: 'Title is ' + (metaTitle.length < 30 ? 'too short' : 'too long') + ' (' + metaTitle.length + ' chars)' });
        }

        // Meta description (120-160 chars ideal)
        seoMax += 10;
        if (metaDescription.length >= 120 && metaDescription.length <= 160) {
            seoScore += 10;
            seoChecks.push({ pass: true, msg: 'Meta description length is ideal (' + metaDescription.length + ' chars)' });
        } else if (metaDescription.length >= 80 && metaDescription.length <= 200) {
            seoScore += 5;
            seoChecks.push({ pass: false, msg: 'Meta description (' + metaDescription.length + ') — aim for 120-160 chars' });
        } else if (metaDescription.length === 0) {
            seoChecks.push({ pass: false, msg: 'Missing meta description' });
        } else {
            seoChecks.push({ pass: false, msg: 'Meta description is ' + (metaDescription.length < 80 ? 'too short' : 'too long') });
        }

        // Slug is clean (no uppercase, no special chars except hyphens)
        seoMax += 5;
        if (slug && /^[a-z0-9]+(-[a-z0-9]+)*$/.test(slug)) {
            seoScore += 5;
            seoChecks.push({ pass: true, msg: 'URL slug is clean and SEO-friendly' });
        } else if (slug) {
            seoScore += 2;
            seoChecks.push({ pass: false, msg: 'URL slug could be cleaner — use lowercase with hyphens' });
        }

        // Has headings (h2 or h3)
        seoMax += 10;
        var headings = content.match(/<h[23][^>]*>/gi) || [];
        if (headings.length >= 2) {
            seoScore += 10;
            seoChecks.push({ pass: true, msg: 'Good heading structure (' + headings.length + ' subheadings)' });
        } else if (headings.length === 1) {
            seoScore += 5;
            seoChecks.push({ pass: false, msg: 'Only 1 subheading — add more to improve structure' });
        } else {
            seoChecks.push({ pass: false, msg: 'No subheadings found — add H2/H3 tags for structure' });
        }

        // Word count (300+ recommended)
        seoMax += 10;
        var plainText = content.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim();
        var words = plainText ? plainText.split(' ') : [];
        var wordCount = words.length;
        if (wordCount >= 300) {
            seoScore += 10;
            seoChecks.push({ pass: true, msg: 'Good content length (' + wordCount + ' words)' });
        } else if (wordCount >= 150) {
            seoScore += 5;
            seoChecks.push({ pass: false, msg: 'Content is short (' + wordCount + ' words) — aim for 300+' });
        } else {
            seoChecks.push({ pass: false, msg: 'Very short content (' + wordCount + ' words) — 300+ recommended' });
        }

        // Has tags
        seoMax += 5;
        if (tags.length >= 2) {
            seoScore += 5;
            seoChecks.push({ pass: true, msg: 'Post has ' + tags.length + ' tags' });
        } else if (tags.length === 1) {
            seoScore += 3;
            seoChecks.push({ pass: false, msg: 'Only 1 tag — add more for discoverability' });
        } else {
            seoChecks.push({ pass: false, msg: 'No tags — add tags for categorization' });
        }

        // --- Readability Scoring ---
        var readChecks = [];
        var readScore = 0;
        var readMax = 0;

        // Average sentence length (15-20 words ideal)
        readMax += 10;
        var sentences = plainText.split(/[.!?]+/).filter(function (s) { return s.trim().length > 0; });
        var avgSentenceLen = sentences.length > 0 ? Math.round(wordCount / sentences.length) : 0;
        if (avgSentenceLen >= 10 && avgSentenceLen <= 20) {
            readScore += 10;
            readChecks.push({ pass: true, msg: 'Good sentence length (avg ' + avgSentenceLen + ' words)' });
        } else if (avgSentenceLen > 0 && avgSentenceLen <= 25) {
            readScore += 6;
            readChecks.push({ pass: false, msg: 'Sentences avg ' + avgSentenceLen + ' words — aim for 15-20' });
        } else if (avgSentenceLen > 25) {
            readChecks.push({ pass: false, msg: 'Sentences are long (avg ' + avgSentenceLen + ') — break them up' });
        }

        // Paragraph count (short paragraphs are better for web)
        readMax += 10;
        var paragraphs = content.match(/<p[^>]*>/gi) || [];
        if (paragraphs.length >= 3 && wordCount > 0) {
            var avgParaWords = Math.round(wordCount / paragraphs.length);
            if (avgParaWords <= 100) {
                readScore += 10;
                readChecks.push({ pass: true, msg: 'Good paragraph length (avg ' + avgParaWords + ' words)' });
            } else {
                readScore += 5;
                readChecks.push({ pass: false, msg: 'Paragraphs are long (avg ' + avgParaWords + ' words) — break them up' });
            }
        } else if (paragraphs.length > 0) {
            readScore += 5;
            readChecks.push({ pass: false, msg: 'Only ' + paragraphs.length + ' paragraphs — use more for readability' });
        }

        // Flesch-Kincaid approximation (syllable estimate)
        readMax += 10;
        var syllables = 0;
        for (var w = 0; w < words.length; w++) {
            var word = words[w].toLowerCase().replace(/[^a-z]/g, '');
            if (word.length <= 3) { syllables += 1; continue; }
            word = word.replace(/(?:[^laeiouy]es|ed|[^laeiouy]e)$/, '');
            word = word.replace(/^y/, '');
            var vowelGroups = word.match(/[aeiouy]{1,2}/g);
            syllables += (vowelGroups ? vowelGroups.length : 1);
        }
        var fk = 0;
        if (sentences.length > 0 && wordCount > 0) {
            fk = 206.835 - (1.015 * (wordCount / sentences.length)) - (84.6 * (syllables / wordCount));
        }
        if (fk >= 60) {
            readScore += 10;
            readChecks.push({ pass: true, msg: 'Easy to read (Flesch score: ' + Math.round(fk) + ')' });
        } else if (fk >= 40) {
            readScore += 6;
            readChecks.push({ pass: false, msg: 'Moderately readable (Flesch score: ' + Math.round(fk) + ')' });
        } else if (wordCount > 0) {
            readChecks.push({ pass: false, msg: 'Difficult to read (Flesch score: ' + Math.round(fk) + ') — simplify language' });
        }

        // Compute percentages
        var seoPct = seoMax > 0 ? Math.round((seoScore / seoMax) * 100) : 0;
        var readPct = readMax > 0 ? Math.round((readScore / readMax) * 100) : 0;
        var overallPct = Math.round((seoPct + readPct) / 2);

        function scoreColor(pct) {
            if (pct >= 70) return '#059669';
            if (pct >= 40) return '#D97706';
            return '#DC2626';
        }

        function scoreLabel(pct) {
            if (pct >= 70) return 'Good';
            if (pct >= 40) return 'OK';
            return 'Needs Work';
        }

        // --- Inject Enhanced JSON-LD ---
        var existingLd = doc.querySelector('script[type="application/ld+json"]');
        if (!existingLd) {
            var ldScript = doc.createElement('script');
            ldScript.setAttribute('type', 'application/ld+json');
            var ldData = {
                '@context': 'https://schema.org',
                '@type': 'Article',
                'headline': metaTitle,
                'description': metaDescription,
                'wordCount': wordCount,
                'articleSection': tags.length > 0 ? tags[0] : '',
                'keywords': tags.join(', ')
            };
            if (context.author) ldData.author = { '@type': 'Person', 'name': context.author };
            if (context.publishedAt) ldData.datePublished = context.publishedAt;
            if (context.coverImageUrl) ldData.image = context.coverImageUrl;
            ldScript.textContent = JSON.stringify(ldData);
            head.appendChild(ldScript);
        }

        // --- Inject Floating Widget ---
        var widget = doc.createElement('div');
        widget.id = 'seo-analyzer-widget';
        body.appendChild(widget);

        var style = doc.createElement('style');
        style.textContent = ''
            + '#seo-analyzer-widget {'
            + '  position: fixed; bottom: 20px; right: 20px; z-index: 99998;'
            + '  font-family: system-ui, sans-serif; font-size: 13px;'
            + '}'
            + '#seo-analyzer-toggle {'
            + '  width: 48px; height: 48px; border-radius: 50%; border: 3px solid ' + scoreColor(overallPct) + ';'
            + '  background: white; color: ' + scoreColor(overallPct) + '; font-weight: 700; font-size: 14px;'
            + '  cursor: pointer; display: flex; align-items: center; justify-content: center;'
            + '  box-shadow: 0 2px 12px rgba(0,0,0,0.15); transition: transform 0.15s;'
            + '}'
            + '#seo-analyzer-toggle:hover { transform: scale(1.1); }'
            + '#seo-analyzer-panel {'
            + '  display: none; position: absolute; bottom: 56px; right: 0;'
            + '  width: 320px; max-height: 420px; overflow-y: auto;'
            + '  background: white; border: 1px solid #E5E5E3; border-radius: 8px;'
            + '  box-shadow: 0 8px 24px rgba(0,0,0,0.12); padding: 16px;'
            + '}'
            + '#seo-analyzer-panel.open { display: block; }'
            + '.seo-section { margin-bottom: 12px; }'
            + '.seo-section-title { font-weight: 600; font-size: 12px; text-transform: uppercase; letter-spacing: 0.05em; color: #6B6B6B; margin-bottom: 6px; }'
            + '.seo-check { display: flex; align-items: flex-start; gap: 6px; padding: 3px 0; font-size: 12px; color: #3A3A3A; }'
            + '.seo-dot { width: 8px; height: 8px; border-radius: 50%; margin-top: 3px; flex-shrink: 0; }'
            + '.seo-score-bar { height: 6px; border-radius: 3px; background: #E5E5E3; margin-top: 4px; margin-bottom: 8px; }'
            + '.seo-score-fill { height: 100%; border-radius: 3px; transition: width 0.3s; }';
        head.appendChild(style);

        function renderChecks(checks) {
            var html = '';
            for (var i = 0; i < checks.length; i++) {
                var color = checks[i].pass ? '#059669' : '#D97706';
                html += '<div class="seo-check"><div class="seo-dot" style="background:' + color + '"></div><span>' + checks[i].msg + '</span></div>';
            }
            return html;
        }

        widget.innerHTML = ''
            + '<div id="seo-analyzer-panel">'
            + '  <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:12px;">'
            + '    <span style="font-weight:700;font-size:14px;color:#1A1A1A;">SEO Analyzer</span>'
            + '    <span style="font-weight:600;color:' + scoreColor(overallPct) + ';">' + overallPct + '% ' + scoreLabel(overallPct) + '</span>'
            + '  </div>'
            + '  <div class="seo-section">'
            + '    <div class="seo-section-title">SEO Score: ' + seoPct + '%</div>'
            + '    <div class="seo-score-bar"><div class="seo-score-fill" style="width:' + seoPct + '%;background:' + scoreColor(seoPct) + ';"></div></div>'
            + renderChecks(seoChecks)
            + '  </div>'
            + '  <div class="seo-section">'
            + '    <div class="seo-section-title">Readability: ' + readPct + '%</div>'
            + '    <div class="seo-score-bar"><div class="seo-score-fill" style="width:' + readPct + '%;background:' + scoreColor(readPct) + ';"></div></div>'
            + renderChecks(readChecks)
            + '  </div>'
            + '</div>'
            + '<div id="seo-analyzer-toggle">' + overallPct + '</div>';

        // Toggle panel on click
        var toggle = doc.getElementById('seo-analyzer-toggle');
        var panel = doc.getElementById('seo-analyzer-panel');
        if (toggle && panel) {
            toggle.addEventListener('click', function () {
                panel.classList.toggle('open');
            });
        }
    });
})();
