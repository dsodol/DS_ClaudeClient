(function() {
    'use strict';

    // Multi-line entry enhancement
    function enhanceTextarea() {
        const textareas = document.querySelectorAll('textarea, [contenteditable="true"], div[role="textbox"]');
        textareas.forEach(textarea => {
            if (textarea.dataset.dsClaudeClientEnhanced) return;
            textarea.dataset.dsClaudeClientEnhanced = 'true';

            // Make textarea expandable
            if (textarea.tagName === 'TEXTAREA') {
                textarea.style.minHeight = '100px';
                textarea.style.maxHeight = '400px';
                textarea.style.resize = 'vertical';
                textarea.style.overflowY = 'auto';
            }

            // Handle Shift+Enter for new line (prevent form submission)
            textarea.addEventListener('keydown', function(e) {
                if (e.key === 'Enter' && e.shiftKey) {
                    e.stopPropagation();
                    // Allow default behavior for new line
                }
            }, true);

            // Auto-expand contenteditable divs
            if (textarea.getAttribute('contenteditable') === 'true' || textarea.getAttribute('role') === 'textbox') {
                textarea.addEventListener('input', function() {
                    this.style.height = 'auto';
                    this.style.height = Math.min(this.scrollHeight, 400) + 'px';
                });
            }
        });
    }

    // Run on page load and observe for dynamic content
    enhanceTextarea();

    const observer = new MutationObserver(() => {
        enhanceTextarea();
    });

    observer.observe(document.body, {
        childList: true,
        subtree: true
    });

    // Expose function to insert snippet text
    window.insertSnippetText = function(text) {
        // Try to find Claude's input area
        const selectors = [
            'textarea[placeholder*="Message"]',
            'textarea[data-testid]',
            'div[contenteditable="true"][role="textbox"]',
            'div[contenteditable="true"]',
            'textarea'
        ];

        let targetInput = null;

        for (const selector of selectors) {
            const elements = document.querySelectorAll(selector);
            for (const element of elements) {
                // Check if this looks like a main input (not a small field)
                if (element.offsetHeight > 30 || element.closest('form')) {
                    targetInput = element;
                    break;
                }
            }
            if (targetInput) break;
        }

        if (!targetInput) {
            // Fallback: get any visible textarea or contenteditable
            const all = document.querySelectorAll('textarea, [contenteditable="true"]');
            for (const el of all) {
                if (el.offsetParent !== null) { // visible
                    targetInput = el;
                    break;
                }
            }
        }

        if (targetInput) {
            targetInput.focus();

            if (targetInput.tagName === 'TEXTAREA' || targetInput.tagName === 'INPUT') {
                const start = targetInput.selectionStart || 0;
                const end = targetInput.selectionEnd || 0;
                const value = targetInput.value || '';
                targetInput.value = value.substring(0, start) + text + value.substring(end);
                targetInput.selectionStart = targetInput.selectionEnd = start + text.length;

                // Trigger React's synthetic event
                const nativeInputValueSetter = Object.getOwnPropertyDescriptor(window.HTMLTextAreaElement.prototype, 'value').set;
                nativeInputValueSetter.call(targetInput, targetInput.value);

                targetInput.dispatchEvent(new Event('input', { bubbles: true }));
                targetInput.dispatchEvent(new Event('change', { bubbles: true }));
            } else {
                // ContentEditable or div with role="textbox"
                const selection = window.getSelection();

                // Insert at cursor or at end
                if (selection.rangeCount > 0 && targetInput.contains(selection.anchorNode)) {
                    const range = selection.getRangeAt(0);
                    range.deleteContents();

                    // Handle multi-line text
                    const lines = text.split('\n');
                    for (let i = 0; i < lines.length; i++) {
                        if (i > 0) {
                            range.insertNode(document.createElement('br'));
                            range.collapse(false);
                        }
                        range.insertNode(document.createTextNode(lines[i]));
                        range.collapse(false);
                    }
                } else {
                    // Append at end
                    const lines = text.split('\n');
                    for (let i = 0; i < lines.length; i++) {
                        if (i > 0) {
                            targetInput.appendChild(document.createElement('br'));
                        }
                        targetInput.appendChild(document.createTextNode(lines[i]));
                    }
                }

                // Trigger input event
                targetInput.dispatchEvent(new Event('input', { bubbles: true }));
            }

            return true;
        }

        console.warn('DS Claude Client: Could not find input element');
        return false;
    };

    console.log('DS Claude Client enhancements loaded successfully');
})();
