var express = require('express');
var path = require('path');
var cla = require('./../api/cla');
var logger = require('./../services/logger');
//////////////////////////////////////////////////////////////////////////////////////////////
// Default router
//////////////////////////////////////////////////////////////////////////////////////////////

var router = express.Router();

// router.use('/accept', function(req, res) {
router.use('/accept/:owner/:repo', function(req, res) {
	req.args = {owner: req.params.owner, repo: req.params.repo};

    if (req.isAuthenticated()) {
		cla.sign(req, function(err){
			if (err) {
				logger.error(err);
			}
			var redirectUrl = path.join(path.sep, req.args.owner, req.args.repo);
			redirectUrl = req.query.pullRequest ? redirectUrl + '?pullRequest=' + req.query.pullRequest : redirectUrl;
			res.redirect(redirectUrl);
		});

    } else {
		req.session.next = req.originalUrl;
		return res.redirect('/auth/github');
    }
});

router.all('/static/*', function(req, res) {
	var filePath;
	if (req.user && req.path === '/static/cla-assistant.json') {
		filePath = path.join(__dirname, '..', '..', '..', 'cla-assistant.json');
	}
	else {
		filePath = path.join(__dirname, '..', '..', 'client', 'login.html');
	}
	res.setHeader('Last-Modified', (new Date()).toUTCString());
	res.status(200).sendFile(filePath);
});

router.all('/*', function(req, res) {
	var filePath;
	if (req.user || req.path !== '/') {
		filePath = path.join(__dirname, '..', '..', 'client', 'home.html');
	}
	else {
		filePath = path.join(__dirname, '..', '..', 'client', 'login.html');
	}
	res.setHeader('Last-Modified', (new Date()).toUTCString());
	res.status(200).sendFile(filePath);
});

module.exports = router;
