export default function (view) {
    var output = view.querySelector('#terminal-output');
    var input = view.querySelector('#terminal-input');
    var cwdInput = view.querySelector('#cwd-input');
    var history = [];
    var historyIndex = -1;
    var running = false;

    function escapeHtml(text) {
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function appendOutput(html) {
        output.innerHTML += html;
        output.scrollTop = output.scrollHeight;
    }

    function executeCommand(command) {
        if (running) return;
        running = true;

        appendOutput(
            '<div><span class="cmd-line">&gt; </span><span class="cmd-text">' +
            escapeHtml(command) + '</span></div>'
        );

        var url = window.ApiClient.getUrl('HelloWorld/Execute');
        var payload = JSON.stringify({
            command: command,
            workingDirectory: cwdInput.value || 'C:\\'
        });

        window.ApiClient.ajax({
            type: 'POST',
            url: url,
            data: payload,
            contentType: 'application/json',
            dataType: 'json'
        }).then(function (result) {
            if (result.output) {
                appendOutput('<div class="stdout">' + escapeHtml(result.output) + '</div>');
            }
            if (result.error) {
                appendOutput('<div class="stderr">' + escapeHtml(result.error) + '</div>');
            }

            var exitClass = result.exitCode === 0 ? 'exit-code' : 'exit-code error';
            appendOutput(
                '<div class="' + exitClass + '">[exit: ' + result.exitCode + ']</div><br/>'
            );

            running = false;
            input.focus();
        }, function (err) {
            appendOutput(
                '<div class="stderr">Request failed: ' + escapeHtml(String(err)) + '</div><br/>'
            );
            running = false;
            input.focus();
        });
    }

    input.addEventListener('keydown', function (e) {
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
        input.focus();
        if (output.children.length === 0) {
            appendOutput(
                '<div class="stdout">Web Terminal ready. Commands run as the Jellyfin service account.</div>' +
                '<div class="stdout">Type a command and press Enter.</div><br/>'
            );
        }
    });
}
