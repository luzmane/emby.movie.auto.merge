define(['loading', 'globalize', 'emby-button', 'emby-checkbox', 'emby-multilineselect'], function (loading) {
  'use strict';
  const pluginId = '7f6902cc-a3ba-40d9-868f-98f73291fdf7';

  async function streamToText(stream) {
    return await new Response(stream).text();
  }

  function splitCollections(e) {
    const provList = this.querySelector("#provList");
    const providerType = provList.options[provList.selectedIndex].text;
    const providerId = this.querySelector('.providerId').value;
    const response = ApiClient.ajax({type: 'get', url: ApiClient.getUrl("MergeMovies/Split/" + providerType + "/" + providerId)});
    response.then((value) => {
      streamToText(value.body)
        .then((res) => {
          if ('true' === res)
            Dashboard.alert("Movie with Provider: " + providerType + ", ID: " + providerId + " split");
          else
            Dashboard.alert("Movie with Provider: " + providerType + ", ID: " + providerId + " not found or is not versioned");
        });
    });
  }

  function loadPage(page, config) {
    page.querySelector('.chkMergeAcrossLibraries').checked = config.MergeAcrossLibraries;
    page.querySelector('.chkDoNotChangeLockedItems').checked = config.DoNotChangeLockedItems;
    page.querySelector('.chkRunAutomatically').checked = config.RunAutomatically;
    const provList = page.querySelector('#provList');
    config.ProvidersList.forEach(name => {
      let option = document.createElement("option");
      option.innerText = name;
      provList.appendChild(option);
    });
    const template = page.querySelector('div.pluginConfigurationPage label.providerTemplate');
    config.ProvidersList.forEach(provider => {
      const checkbox = template.cloneNode(true);
      template.parentNode.appendChild(checkbox);
      checkbox.removeAttribute("id");
      checkbox.removeAttribute("style");
      checkbox.classList.remove('providerTemplate');
      const input = checkbox.querySelector('input[is="emby-checkbox"]');
      input.checked = config.UsedProviders.includes(provider);
      input.setAttribute('category', provider);
      const desc = checkbox.querySelector('.checkboxButtonLabel');
      desc.innerText = provider;
    });
    loading.hide();
  }

  function onSubmit(e) {
    e.preventDefault();
    loading.show();
    const form = this;
    getConfig().then(function (config) {
      config.MergeAcrossLibraries = form.querySelector('.chkMergeAcrossLibraries').checked;
      config.DoNotChangeLockedItems = form.querySelector('.chkDoNotChangeLockedItems').checked;
      config.RunAutomatically = form.querySelector('.chkRunAutomatically').checked;
      const chosen = form.querySelectorAll('input.provider:checked');
      chosen.forEach(l => console.log(l));
      config.UsedProviders = [];
      for (let key of chosen) {
        config.UsedProviders.push(key.attributes['category'].textContent);
      }
      ApiClient.updatePluginConfiguration(pluginId, config).then(Dashboard.processServerConfigurationUpdateResult);
    });
    return false;
  }

  function getConfig() {
    return ApiClient.getPluginConfiguration(pluginId);
  }

  return function (view, params) {
    view.querySelector('form.mergeConfigForm').addEventListener('submit', onSubmit);
    view.querySelector('form.splitForm').addEventListener('submit', splitCollections);
    view.addEventListener('viewshow', function () {
      loading.show();
      const page = this;
      getConfig().then(function (config) {
        loadPage(page, config);
      });
    });
  };
});
