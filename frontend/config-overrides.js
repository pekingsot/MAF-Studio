module.exports = function override(config, env) {
  config.devServer = {
    ...config.devServer,
    allowedHosts: 'all',
  };
  return config;
};