module.exports = {
	configureWebpack: {
		devServer: {
			proxy: {
				'^/api': {
					target: "https://localhost:44369"
				}
			}
		}
	}
};