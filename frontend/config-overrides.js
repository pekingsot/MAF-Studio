const { override, overrideDevServer, addBabelPlugins, useBabelRc } = require('customize-cra');

module.exports = {
  webpack: override(
    useBabelRc(),
    ...addBabelPlugins(
      '@babel/plugin-proposal-optional-chaining',
      '@babel/plugin-proposal-nullish-coalescing-operator'
    )
  ),
  devServer: overrideDevServer((config) => {
    config.allowedHosts = 'all';
    return config;
  }),
  jest: function(config) {
    config.transformIgnorePatterns = [
      'node_modules/(?!(axios|reactflow)/)',
    ];
    return config;
  },
};
