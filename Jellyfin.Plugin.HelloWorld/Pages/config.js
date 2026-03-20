var WebTerminalConfig = {
    pluginUniqueId: '256a2512-89aa-43f5-bbc1-2157a5647c3a'
};

function generateKey() {
    var chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
    var key = '';
    var array = new Uint8Array(32);
    crypto.getRandomValues(array);
    for (var i = 0; i < 32; i++) {
        key += chars[array[i] % chars.length];
    }
    return key;
}

document.querySelector('#WebTerminalConfigPage')
    .addEventListener('pageshow', function () {
        Dashboard.showLoadingMsg();
        ApiClient.getPluginConfiguration(WebTerminalConfig.pluginUniqueId).then(function (config) {
            document.querySelector('#txtApiKey').value = config.ApiKey || '';
            document.querySelector('#txtShellPath').value = config.ShellPath || 'cmd.exe';
            document.querySelector('#txtShellArgs').value = config.ShellArgs || '/c';
            document.querySelector('#txtTimeout').value = config.CommandTimeoutSeconds || 300;
            Dashboard.hideLoadingMsg();
        });
    });

document.querySelector('#btnToggleKey')
    .addEventListener('click', function () {
        var input = document.querySelector('#txtApiKey');
        var btn = this;
        if (input.type === 'password') {
            input.type = 'text';
            btn.textContent = 'Hide';
        } else {
            input.type = 'password';
            btn.textContent = 'Show';
        }
    });

document.querySelector('#btnRegenKey')
    .addEventListener('click', function () {
        if (confirm('Generate a new API key? The old key will stop working immediately after saving.')) {
            document.querySelector('#txtApiKey').value = generateKey();
            document.querySelector('#txtApiKey').type = 'text';
            document.querySelector('#btnToggleKey').textContent = 'Hide';
        }
    });

document.querySelector('#WebTerminalConfigForm')
    .addEventListener('submit', function (e) {
        Dashboard.showLoadingMsg();
        ApiClient.getPluginConfiguration(WebTerminalConfig.pluginUniqueId).then(function (config) {
            config.ApiKey = document.querySelector('#txtApiKey').value;
            config.ShellPath = document.querySelector('#txtShellPath').value;
            config.ShellArgs = document.querySelector('#txtShellArgs').value;
            config.CommandTimeoutSeconds = parseInt(document.querySelector('#txtTimeout').value, 10) || 300;
            ApiClient.updatePluginConfiguration(WebTerminalConfig.pluginUniqueId, config).then(function (result) {
                Dashboard.processPluginConfigurationUpdateResult(result);
            });
        });
        e.preventDefault();
        return false;
    });
