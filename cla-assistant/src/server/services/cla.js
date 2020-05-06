// models
require('../documents/cla');
// require('../documents/user');
var https = require('https');
var q = require('q');
var CLA = require('mongoose').model('CLA');

//services
var repoService = require('../services/repo');
var logger = require('../services/logger');

module.exports = function(){
	var claService;

	var	checkAll = function (users, args) {
		var deferred = q.defer();
		var all_signed = true;
		var promises = [];
		var user_map = {signed: [], not_signed: []};
		if (!users) {
			deferred.reject('There are no users to check :( ');
			return deferred.promise;
		}
		users.forEach(function(user){
			args.user = user.name;
			user_map.not_signed.push(args.user);

			promises.push(claService.get(args, function(err, cla){
				if (err) {
					logger.warn(err);
				}
				if (!cla) {
					all_signed = false;
				} else {
					var i = user_map.not_signed.indexOf(cla.user);
					if (i >= 0) {
						user_map.not_signed.splice(i, 1);
					}
					user_map.signed.push(cla.user);
				}
			}));
		});
		q.all(promises).then(function(){
			deferred.resolve({signed: all_signed, user_map: user_map});
		});
		return deferred.promise;
	};


	claService = {
		getGist: function(args, done){
			try{
				var gist_url = args.gist.gist_url || args.gist.url || args.gist;
				var gistArray = gist_url.split('/'); // https://gist.github.com/KharitonOff/60e9b5d7ce65ca474c29

			} catch(ex) {
				done('The gist url "' + gist_url + '" seems to be invalid');
				return;
			}

			var path = '/gists/';
			var id = gistArray[gistArray.length - 1];
			path += id;
			if (args.gist.gist_version) {
				path = path + '/' + args.gist.gist_version;
			}

			var req = {};
			var data = '';
			var options = {
				hostname: config.server.github.api,
				port: 443,
				path: path,
				method: 'GET',
				headers: {
					'Authorization': 'token ' + args.token,
					'User-Agent': 'cla-assistant'
				}
			};

			req = https.request(options, function(res){
				res.on('data', function(chunk) { data += chunk; });
				res.on('end', function(){
					data = JSON.parse(data);
					done(null, data);
				});
			});

			req.end();
			req.on('error', function (e) {
				done(e);
			});
		},
		getRepo: function(args, done) {
			var deferred = q.defer();
			repoService.get(args, function(err, repo){
				if (!err && repo) {
					deferred.resolve(repo);
				}
				if(typeof done === 'function'){
					done(err, repo);
				}
			});
			return deferred.promise;
		},

		get: function(args, done) {
			var deferred = q.defer();
			CLA.findOne({repo: args.repo, owner: args.owner, user: args.user, gist_url: args.gist, gist_version: args.gist_version}, function(err, cla){
				deferred.resolve();
				done(err, cla);
			});
			return deferred.promise;
		},

		//Get last signature of the user for given repository and gist url
		getLastSignature: function(args, done) {
			var deferred = q.defer();
			CLA.findOne({repo: args.repo, owner: args.owner, user: args.user, gist_url: args.gist_url}, {'repo': '*', 'owner': '*', 'created_at': '*', 'gist_url': '*', 'gist_version': '*'}, {select: {'created_at': -1}}, function(err, cla){
				if (!err && cla) {
					deferred.resolve(cla);
				}
				if(typeof done === 'function'){
					done(err, cla);
				}
			});
			return deferred.promise;
		},


		check: function(args, done){
			var self = this;

			this.getRepo(args, function(e, repo){
				if (e || !repo || !repo.gist) {
					done(e, false);
					return;
				}

				args.gist = repo.gist;

				self.getGist(repo, function(err, gist){
					if (err || !gist.history) {
						done(err, false);
						return;
					}
					args.gist_version = gist.history[0].version;

					if (args.user) {
						self.get(args, function(error, cla){
							done(error, !!cla);
						});
					}
					else if (args.number) {
						repoService.getPRCommitters(args, function(error, committers){
							if (error) {
								logger.warn(error);
							}
							checkAll(committers, args).then(function(result){
								done(null, result.signed, result.user_map);
							},
							function(error_msg){
								done(error_msg, false);
							});
						});
					}
				});
			});
		},

		sign: function(args, done) {
			var self = this;

			self.check(args, function(e, signed){
				if (e || signed) {
					done(e);
					return;
				}

				self.getRepo(args, function(err, repo){
					if (err || !repo) {
						done(err);
						return;
					}

					args.gist_url = repo.gist;

					self.create(args, function(error){
						if (error) {
							done(error);
							return;
						}
						done(error, 'done');
					});
				});
			});
		},

		//Get list of signed CLAs for all repos the user has contributed to
		getSignedCLA: function(args, done){
			var selector = [];
			var findCla = function(query, repoList, claList, cb){
				CLA.find(query, {'repo': '*', 'owner': '*', 'created_at': '*', 'gist_url': '*', 'gist_version': '*'}, {sort: {'created_at': -1}}, function(err, clas){
					if (err) {
						logger.warn(err);
					} else {
						clas.forEach(function(cla){
							if(repoList.indexOf(cla.repo) < 0){
								repoList.push(cla.repo);
								claList.push(cla);
							}
						});
					}
					cb();
				});
			};

			repoService.all(function(e, repos){
				if (e) {
					logger.warn(e);
				}
				repos.forEach(function(repo){
					selector.push({
						user: args.user,
						repo: repo.repo,
						gist_url: repo.gist
					});
				});
				var repoList = [];
				var uniqueClaList = [];
				findCla({$or: selector}, repoList, uniqueClaList, function(){
					findCla({user: args.user}, repoList, uniqueClaList, function(){
						done(null, uniqueClaList);
					});
				});
			});
		},

		getAll: function(args, done) {
			var self = this;
			var valid = [];
			if (args.gist.gist_version) {
				CLA.find({repo: args.repo, owner: args.owner, gist_url: args.gist.gist_url, gist_version: args.gist.gist_version}, function(err, clas){
					done(err, clas);
				});
			} else {
				CLA.find({repo: args.repo, owner: args.owner, gist_url: args.gist.gist_url}, function(e, clas){
					if (!clas) {
						done(e, clas);
						return;
					}
					self.getRepo(args, function(err, repo){
						if (err) {
							logger.warn(err);
						}
						self.getGist(repo, function(error, gist){
							if (!gist) {
								done(error, gist);
								return;
							}
							clas.forEach(function(cla){
								if (gist.history && gist.history.length > 0 && gist.history[0].version === cla.gist_version) {
									valid.push(cla);
								}
							});
							done(error, valid);
						});
					});
				});
			}

		},
		create: function(args, done){
			var now = new Date();

			CLA.create({repo: args.repo, owner: args.owner, user: args.user, gist_url: args.gist, gist_version: args.gist_version, created_at: now}, function(err, res){
				done(err, res);
			});
		}
	};
	return claService;
}();
