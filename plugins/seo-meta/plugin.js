// name: "seo-meta"
// version: "1.0.0"
// description: "Adds meta description, Open Graph tags, and JSON-LD Article structured data to rendered posts."
// author: "Contento"
// hooks: ["post:render"]

(function () {
    contento.on('post:render', function (context) {
        var post = context.post;
        var doc = context.document;

        if (!post || !doc) {
            return;
        }

        var head = doc.head || doc.getElementsByTagName('head')[0];
        if (!head) {
            return;
        }

        var title = post.title || '';
        var description = post.excerpt || post.summary || '';
        var url = post.url || '';
        var image = post.featuredImage || post.coverImage || '';
        var author = post.author || '';
        var publishedDate = post.publishedAt || post.createdAt || '';
        var modifiedDate = post.updatedAt || publishedDate;
        var siteName = context.site ? context.site.name : 'Contento';

        // Truncate description to 160 characters for meta tag
        if (description.length > 160) {
            description = description.substring(0, 157) + '...';
        }

        // Helper to create or update a meta tag
        function setMeta(attribute, attributeValue, content) {
            if (!content) {
                return;
            }
            var selector = 'meta[' + attribute + '="' + attributeValue + '"]';
            var existing = doc.querySelector(selector);
            if (existing) {
                existing.setAttribute('content', content);
            } else {
                var meta = doc.createElement('meta');
                meta.setAttribute(attribute, attributeValue);
                meta.setAttribute('content', content);
                head.appendChild(meta);
            }
        }

        // Meta description
        setMeta('name', 'description', description);

        // Open Graph tags
        setMeta('property', 'og:type', 'article');
        setMeta('property', 'og:title', title);
        setMeta('property', 'og:description', description);
        setMeta('property', 'og:url', url);
        setMeta('property', 'og:site_name', siteName);

        if (image) {
            setMeta('property', 'og:image', image);
        }

        if (publishedDate) {
            setMeta('property', 'article:published_time', publishedDate);
        }

        if (modifiedDate) {
            setMeta('property', 'article:modified_time', modifiedDate);
        }

        // Twitter Card tags
        setMeta('name', 'twitter:card', image ? 'summary_large_image' : 'summary');
        setMeta('name', 'twitter:title', title);
        setMeta('name', 'twitter:description', description);

        if (image) {
            setMeta('name', 'twitter:image', image);
        }

        // JSON-LD Article structured data
        var jsonLd = {
            '@context': 'https://schema.org',
            '@type': 'Article',
            'headline': title,
            'description': description,
            'mainEntityOfPage': {
                '@type': 'WebPage',
                '@id': url
            }
        };

        if (image) {
            jsonLd.image = image;
        }

        if (author) {
            jsonLd.author = {
                '@type': 'Person',
                'name': author
            };
        }

        if (siteName) {
            jsonLd.publisher = {
                '@type': 'Organization',
                'name': siteName
            };
        }

        if (publishedDate) {
            jsonLd.datePublished = publishedDate;
        }

        if (modifiedDate) {
            jsonLd.dateModified = modifiedDate;
        }

        var script = doc.createElement('script');
        script.setAttribute('type', 'application/ld+json');
        script.textContent = JSON.stringify(jsonLd);
        head.appendChild(script);
    });
})();
