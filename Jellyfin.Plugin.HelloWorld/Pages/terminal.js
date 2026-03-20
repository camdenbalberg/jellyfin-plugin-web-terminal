export default function (view) {
    var keyOverlay = view.querySelector('#key-overlay');
    var keyInput = view.querySelector('#key-input');
    var keySubmit = view.querySelector('#key-submit');
    var keyError = view.querySelector('#key-error');
    var terminalContainer = view.querySelector('#terminal-container');
    var output = view.querySelector('#terminal-output');
    var input = view.querySelector('#terminal-input');
    var cwdInput = view.querySelector('#cwd-input');
    var history = [];
    var historyIndex = -1;
    var running = false;
    var currentSessionId = null;
    var apiKey = '';

    function escapeHtml(text) {
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function appendOutput(html) {
        output.innerHTML += html;
        output.scrollTop = output.scrollHeight;
    }

    function getAuthHeaders() {
        var headers = {
            'Content-Type': 'application/json',
            'X-Terminal-Key': apiKey
        };
        var token = window.ApiClient.accessToken();
        if (token) {
            headers['Authorization'] = 'MediaBrowser Token="' + token + '"';
        }
        return headers;
    }

    function validateKey(key) {
        var url = window.ApiClient.getUrl('HelloWorld/ValidateKey');
        return fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Terminal-Key': key,
                'Authorization': 'MediaBrowser Token="' + window.ApiClient.accessToken() + '"'
            }
        }).then(function (response) {
            return response.ok;
        });
    }

    function unlockTerminal() {
        var key = keyInput.value.trim();
        if (!key) return;

        keyError.style.display = 'none';
        keySubmit.disabled = true;

        validateKey(key).then(function (valid) {
            keySubmit.disabled = false;
            if (valid) {
                apiKey = key;
                sessionStorage.setItem('wt_key', key);
                keyOverlay.style.display = 'none';
                terminalContainer.style.display = 'flex';
                input.focus();
                if (output.children.length === 0) {
                    appendOutput(
                        '<div class="stdout">Web Terminal ready. Commands run as the Jellyfin service account.</div>' +
                        '<div class="stdout">Output streams in real-time. Ctrl+C to cancel a running command.</div><br/>'
                    );
                }
            } else {
                keyError.style.display = 'block';
                keyInput.focus();
                keyInput.select();
            }
        });
    }

    function cancelCommand() {
        if (!running || !currentSessionId) return;

        var url = window.ApiClient.getUrl('HelloWorld/Cancel');
        fetch(url, {
            method: 'POST',
            headers: getAuthHeaders(),
            body: JSON.stringify({ sessionId: currentSessionId })
        });
    }

    function executeCommand(command) {
        if (running) return;
        running = true;
        currentSessionId = null;

        appendOutput(
            '<div><span class="cmd-line">&gt; </span><span class="cmd-text">' +
            escapeHtml(command) + '</span></div>'
        );

        var url = window.ApiClient.getUrl('HelloWorld/Execute');
        var payload = JSON.stringify({
            command: command,
            workingDirectory: cwdInput.value || 'C:\\'
        });

        fetch(url, {
            method: 'POST',
            headers: getAuthHeaders(),
            body: payload
        }).then(function (response) {
            if (response.status === 403) {
                appendOutput('<div class="stderr">API key rejected. Re-authenticating...</div><br/>');
                running = false;
                sessionStorage.removeItem('wt_key');
                apiKey = '';
                terminalContainer.style.display = 'none';
                keyOverlay.style.display = 'flex';
                keyError.style.display = 'block';
                keyInput.focus();
                return;
            }

            var reader = response.body.getReader();
            var decoder = new TextDecoder();
            var buffer = '';

            function processChunk() {
                reader.read().then(function (result) {
                    if (result.done) {
                        running = false;
                        currentSessionId = null;
                        appendOutput('<br/>');
                        input.focus();
                        return;
                    }

                    buffer += decoder.decode(result.value, { stream: true });
                    var parts = buffer.split('\n\n');
                    buffer = parts.pop() || '';

                    for (var i = 0; i < parts.length; i++) {
                        parseSSE(parts[i]);
                    }

                    processChunk();
                });
            }

            processChunk();
        }, function (err) {
            appendOutput(
                '<div class="stderr">Request failed: ' + escapeHtml(String(err)) + '</div><br/>'
            );
            running = false;
            currentSessionId = null;
            input.focus();
        });
    }

    function parseSSE(raw) {
        var eventType = '';
        var data = '';

        var lines = raw.split('\n');
        for (var i = 0; i < lines.length; i++) {
            var line = lines[i];
            if (line.indexOf('event: ') === 0) {
                eventType = line.substring(7);
            } else if (line.indexOf('data: ') === 0) {
                data += (data ? '\n' : '') + line.substring(6);
            }
        }

        if (eventType === 'session') {
            currentSessionId = data;
        } else if (eventType === 'stdout') {
            appendOutput('<div class="stdout">' + escapeHtml(data) + '</div>');
        } else if (eventType === 'stderr') {
            appendOutput('<div class="stderr">' + escapeHtml(data) + '</div>');
        } else if (eventType === 'error') {
            appendOutput('<div class="stderr">' + escapeHtml(data) + '</div>');
        } else if (eventType === 'exit') {
            var code = parseInt(data, 10);
            var exitClass = code === 0 ? 'exit-code' : 'exit-code error';
            appendOutput('<div class="' + exitClass + '">[exit: ' + code + ']</div>');
            running = false;
            currentSessionId = null;
            input.focus();
        }
    }

    keySubmit.addEventListener('click', unlockTerminal);
    keyInput.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') unlockTerminal();
    });

    input.addEventListener('keydown', function (e) {
        if (e.key === 'c' && e.ctrlKey) {
            e.preventDefault();
            if (running) {
                cancelCommand();
                appendOutput('<div class="stderr">^C</div>');
            }
            return;
        }

        if (e.key === 'Enter') {
            var command = input.value.trim();
            if (!command) return;

            history.push(command);
            historyIndex = history.length;
            input.value = '';

            executeCommand(command);
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            if (historyIndex > 0) {
                historyIndex--;
                input.value = history[historyIndex];
            }
        } else if (e.key === 'ArrowDown') {
            e.preventDefault();
            if (historyIndex < history.length - 1) {
                historyIndex++;
                input.value = history[historyIndex];
            } else {
                historyIndex = history.length;
                input.value = '';
            }
        }
    });

    view.addEventListener('viewshow', function () {
        var savedKey = sessionStorage.getItem('wt_key');
        if (savedKey) {
            validateKey(savedKey).then(function (valid) {
                if (valid) {
                    apiKey = savedKey;
                    keyOverlay.style.display = 'none';
                    terminalContainer.style.display = 'flex';
                    input.focus();
                    if (output.children.length === 0) {
                        appendOutput(
                            '<div class="stdout">Web Terminal ready. Commands run as the Jellyfin service account.</div>' +
                            '<div class="stdout">Output streams in real-time. Ctrl+C to cancel a running command.</div><br/>'
                        );
                    }
                } else {
                    sessionStorage.removeItem('wt_key');
                    keyInput.focus();
                }
            });
        } else {
            keyInput.focus();
        }
    });
}
