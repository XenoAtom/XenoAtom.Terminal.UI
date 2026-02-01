
// Ensure copy-to-clipboard buttons get Bootstrap tooltips
document.addEventListener('DOMContentLoaded', () => {
  const copyBtns = document.querySelectorAll('button.copy-to-clipboard-button');

  copyBtns.forEach(btn => {
    // Ensure tooltip attributes exist
    if (!btn.hasAttribute('data-bs-toggle')) btn.setAttribute('data-bs-toggle', 'tooltip');
    if (!btn.hasAttribute('data-bs-placement')) btn.setAttribute('data-bs-placement', 'left');
    if (!btn.hasAttribute('data-bs-title')) btn.setAttribute('data-bs-title', 'Copy to Clipboard.');

    const tt = bootstrap.Tooltip.getOrCreateInstance(btn);
    const original = btn.getAttribute('data-bs-title') || 'Copy to Clipboard.';

    // Hide tooltip on click (if visible from hover)
    //btn.addEventListener('click', () => tt.hide());

    // React to Prism's data-copy-state changes
    const obs = new MutationObserver(muts => {
      for (const m of muts) {
        if (m.type === 'attributes' && m.attributeName === 'data-copy-state') {
          const state = btn.getAttribute('data-copy-state');

          if (state === 'copy-success') {
            if (typeof tt.setContent === 'function') tt.setContent({ '.tooltip-inner': 'Copied!' });
            else btn.setAttribute('data-bs-title', 'Copied!');
            tt.show();
            // Drop focus so the button doesn’t stay highlighted
            btn.blur();
            setTimeout(() => {
              if (typeof tt.setContent === 'function') tt.setContent({ '.tooltip-inner': original });
              else btn.setAttribute('data-bs-title', original);
              tt.hide();
            }, 1200);
          } else if (state === 'copy-error') {
            if (typeof tt.setContent === 'function') tt.setContent({ '.tooltip-inner': 'Failed to copy!' });
            else btn.setAttribute('data-bs-title', 'Failed to copy!');
            tt.show();
            // Drop focus so the button doesn’t stay highlighted
            btn.blur();
            setTimeout(() => {
              if (typeof tt.setContent === 'function') tt.setContent({ '.tooltip-inner': original });
              else btn.setAttribute('data-bs-title', original);
              tt.hide();
            }, 1500);
          }
        }
      }
    });
    obs.observe(btn, { attributes: true, attributeFilter: ['data-copy-state'] });
  });
});

// Initialize bootstrap tooltips
const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]')
const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl))

var jstoc = document.getElementsByClassName("js-toc");
if (jstoc.length > 0)
{
    tocbot.init({
        // Where to render the table of contents.
        tocSelector: '.js-toc',
        // Where to grab the headings to build the table of contents.
        contentSelector: '.js-toc-content',
        // Which headings to grab inside of the contentSelector element.
        headingSelector: 'h2, h3, h4',
        collapseDepth: 3,
        // Ensure correct positioning
        hasInnerContainers: true,
    });
}

var searchInput = document.getElementById("search-input");
var searchMenu = document.getElementById("search-results");
if (searchInput && searchMenu) {
    // Enable deep-link anchors only when search is present
    anchors.add(".xenoatom-docs h2");

    // Container
    const container = searchInput.closest('.xenoatom-search') || searchInput.parentElement;

    // Bootstrap Dropdown controller (guard if Bootstrap JS not present)
    var dropdown = (window.bootstrap && bootstrap.Dropdown)
        ? new bootstrap.Dropdown(searchInput, { autoClose: 'outside', display: 'static' })
        : null;

    let activeIndex = -1;
    let items = [];
    let lastQueryId = 0;

    function clearMenu() {
        searchMenu.innerHTML = '';
        items = [];
        activeIndex = -1;
    }

    function showMenu() {
        if (dropdown) {
            if (!searchMenu.classList.contains('show')) dropdown.show();
        } else {
            searchMenu.classList.add('show');
        }
        searchInput.setAttribute('aria-expanded', 'true');
    }

    function hideMenu() {
        if (dropdown) {
            if (searchMenu.classList.contains('show')) dropdown.hide();
        } else {
            searchMenu.classList.remove('show');
        }
        searchInput.setAttribute('aria-expanded', 'false');
    }

    function setActive(index) {
        if (index < -1 || index >= items.length) return;
        if (activeIndex >= 0 && items[activeIndex]) items[activeIndex].classList.remove('active');
        activeIndex = index;
        if (activeIndex >= 0 && items[activeIndex]) {
            items[activeIndex].classList.add('active');
            items[activeIndex].scrollIntoView({ block: 'nearest' });
        }
    }

    function renderMessage(message) {
        clearMenu();
        const li = document.createElement('li');
        const span = document.createElement('span');
        span.className = 'dropdown-item-text text-body-secondary';
        span.textContent = message;
        li.appendChild(span);
        searchMenu.appendChild(li);
        showMenu();
    }

    function renderRows(rows) {
        clearMenu();
        if (!rows || rows.length === 0) {
            renderMessage('No results found.');
            return;
        }
        const frag = document.createDocumentFragment();
        rows.forEach((row, i) => {
            const li = document.createElement('li');
            li.innerHTML = `<a class="dropdown-item" href="${row.url}">
                              <div class="fw-semibold">${row.title}</div>
                              <div class="small text-body-secondary">${row.snippet}</div>
                            </a>`;
            li.addEventListener('mouseover', () => setActive(i));
            frag.appendChild(li);
        });
        searchMenu.appendChild(frag);
        items = Array.from(searchMenu.querySelectorAll('.dropdown-item'));
        setActive(-1);
        showMenu();
    }

    function performSearch(term) {
        const queryId = ++lastQueryId;
        const q = (term || '').trim();
        if (!q) {
            clearMenu();
            renderMessage('Enter words to search...');
            return;
        }
        DefaultLunetSearch.query(q).then(rows => {
            if (queryId !== lastQueryId) return; // stale
            renderRows(rows);
        }).catch(() => {
            if (queryId !== lastQueryId) return;
            renderMessage('No results found.');
        });
    }

    // Events
    searchInput.addEventListener('input', () => {
        performSearch(searchInput.value);
    });

    searchInput.addEventListener('keydown', (e) => {
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            if (!searchMenu.classList.contains('show')) showMenu();
            setActive(Math.min(activeIndex + 1, items.length - 1));
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            setActive(Math.max(activeIndex - 1, -1));
        } else if (e.key === 'Enter') {
            if (activeIndex >= 0 && items[activeIndex]) {
                e.preventDefault();
                items[activeIndex].click();
            }
        } else if (e.key === 'Escape') {
            hideMenu();
        }
    });

    // Alt+S to focus
    document.addEventListener('keydown', (e) => {
        if (e.altKey && (e.key === 's' || e.key === 'S')) {
            searchInput.focus();
            if (searchInput.value && !searchMenu.classList.contains('show')) showMenu();
            e.preventDefault();
        }
    });

    // Show helper on focus
    searchInput.addEventListener('focus', () => {
        if (!searchInput.value) renderMessage('Enter words to search...');
        else showMenu();
    });

    // Click-away handling if Bootstrap isn't managing it
    if (!dropdown) {
        document.addEventListener('click', (e) => {
            if (container && !container.contains(e.target)) hideMenu();
        });
    }
}

