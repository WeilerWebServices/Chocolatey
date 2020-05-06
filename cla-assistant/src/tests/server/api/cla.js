/*global describe, it, beforeEach, afterEach*/

// unit test
var assert = require('assert');
var sinon = require('sinon');

// config
global.config = require('../../../config');

// models
var Repo = require('../../../server/documents/repo').Repo;

//services
var github = require('../../../server/services/github');
var url = require('../../../server/services/url');
var cla = require('../../../server/services/cla');
var repo_service = require('../../../server/services/repo');
var statusService = require('../../../server/services/status');
var prService = require('../../../server/services/pullRequest');
var log = require('../../../server/services/logger');

// api
var cla_api = require('../../../server/api/cla');

describe('cla:get', function() {
    it('should get gist and render it with user token', function(it_done) {
        sinon.stub(cla, 'getRepo', function(args, cb){
            assert.deepEqual(args, {repo: 'myRepo', owner: 'login'});
            cb(null, {gist: 'url', token: 'repo_token'});
        });
        sinon.stub(cla, 'getGist', function(repo, cb){
            assert.equal(repo.gist.gist_url, 'url');
            var res = {url: 'url', files: {xyFile: {content: 'some content'}}, updated_at: '2011-06-20T11:34:15Z', history: [{version: 'xyz'}]};
            cb(null, res);
        });
        var githubStub = sinon.stub(github, 'call', function(args, cb) {
            var res;
            assert.equal(args.obj, 'markdown');
            assert.equal(args.fun, 'render');
            assert.equal(args.token, 'user_token');
            res = {statusCode: 200, data: {}};
            cb(null, res);
        });

        var req = {args: {repo: 'myRepo', owner: 'login'}, user: {token: 'user_token'}};

        cla_api.get(req, function() {
            assert(cla.getRepo.called);

            githubStub.restore();
            cla.getRepo.restore();
            cla.getGist.restore();
            it_done();
        });
    });

    it('should get gist and render it with repo token', function(it_done) {
        sinon.stub(cla, 'getRepo', function(args, cb){
            assert.deepEqual(args, {repo: 'myRepo', owner: 'login'});
            cb(null, {gist: 'url', token: 'repo_token'});
        });
        sinon.stub(cla, 'getGist', function(repo, cb){
            assert.equal(repo.gist.gist_url, 'url');
            var res = {url: 'url', files: {xyFile: {content: 'some content'}}, updated_at: '2011-06-20T11:34:15Z', history: [{version: 'xyz'}]};
            cb(null, res);
        });
        var githubStub = sinon.stub(github, 'call', function(args, cb) {
            var res;
            assert.equal(args.obj, 'markdown');
            assert.equal(args.fun, 'render');
            assert.equal(args.token, 'repo_token');
            res = {statusCode: 200};
            cb(null, res);
        });

        var req = {args: {repo: 'myRepo', owner: 'login'}};

        cla_api.get(req, function() {
            assert(cla.getRepo.called);

            githubStub.restore();
            cla.getRepo.restore();
            cla.getGist.restore();
            it_done();
        });
    });

    it('should get gist and render it without user token', function(it_done) {
        sinon.stub(cla, 'getRepo', function(args, cb){
            assert.deepEqual(args, {repo: 'myRepo', owner: 'login'});
            // cb(null, {gist: 'url', token: 'abc'});
            cb(null, {gist: 'url'});
        });
        sinon.stub(cla, 'getGist', function(repo, cb){
            assert.equal(repo.gist.gist_url, 'url');
            var res = {url: 'url', files: {xyFile: {content: 'some content'}}, updated_at: '2011-06-20T11:34:15Z', history: [{version: 'xyz'}]};
            cb(null, res);
        });
        var githubStub = sinon.stub(github, 'call', function(args, cb) {
            var res;
            assert.equal(args.obj, 'markdown');
            assert.equal(args.fun, 'render');
            assert.ifError(args.token);
            res = {statusCode: 200};
            cb(null, res);
        });

        var req = {args: {repo: 'myRepo', owner: 'login'}};

        cla_api.get(req, function() {
            assert(cla.getRepo.called);

            githubStub.restore();
            cla.getRepo.restore();
            cla.getGist.restore();
            it_done();
        });
    });

    it('should handle wrong gist url', function(it_done) {

        var repoStub = sinon.stub(Repo, 'findOne', function(args, cb){
            var repo = {repo: 'myRepo', owner: 'login', gist: '123', token: 'abc'};
            cb(null, repo);
        });
        sinon.stub(cla, 'getGist', function(repo, cb){
            cb('error');
        });

        var githubStub = sinon.stub(github, 'call', function() {
            assert();
        });

        var req = {args: {repo: 'myRepo', owner: 'login'}};

        cla_api.get(req, function(error) {
            assert.equal(!!error, true);
            githubStub.restore();
            repoStub.restore();
            cla.getGist.restore();
            it_done();
        });

    });

    it('should handle result with no files', function(it_done) {
        sinon.stub(cla, 'getRepo', function(args, cb){
            assert.deepEqual(args, {repo: 'myRepo', owner: 'login'});
            cb(null, {gist: 'url', token: 'abc'});
        });
        sinon.stub(cla, 'getGist', function(repo, cb){
            assert.equal(repo.gist.gist_url, 'url');
            var res = {url: 'url', updated_at: '2011-06-20T11:34:15Z', history: [{version: 'xyz'}]};
            cb(null, res);
        });

        var req = {args: {repo: 'myRepo', owner: 'login'}};

        cla_api.get(req, function() {
            assert(cla.getRepo.called);

            cla.getRepo.restore();
            cla.getGist.restore();
            it_done();
        });

    });

    describe('in case of failing github api', function(){
        var githubError;
        var githubResponse;
        var req = {args: {repo: 'myRepo', owner: 'login'}, user: {token: 'abc'}};

        beforeEach(function(){
            sinon.stub(cla, 'getRepo', function(args, cb){
                assert.deepEqual(args, {repo: 'myRepo', owner: 'login'});
                cb(null, {gist: 'url', token: 'abc'});
            });
            sinon.stub(cla, 'getGist', function(repo, cb){
                assert.equal(repo.gist.gist_url, 'url');
                var res = {url: 'url', files: {xyFile: {content: 'some content'}}, updated_at: '2011-06-20T11:34:15Z', history: [{version: 'xyz'}]};
                cb(null, res);
            });
            sinon.stub(github, 'call', function(args, cb) {
                cb(githubError, githubResponse);
            });
            sinon.stub(log, 'error', function(error) {
                assert(error);
            });
        });

        afterEach(function(){
            cla.getRepo.restore();
            cla.getGist.restore();
            log.error.restore();
            github.call.restore();
        });

        it('should handle github error', function(it_done){
            githubError = 'any error';
            cla_api.get(req, function(error) {

                assert(error);
                it_done();
            });
        });

        it('should handle error stored in response message', function(it_done){
            githubResponse = {statusCode: 500, message: 'somthing went wrong, e.g. user revoked access rights'};
            githubError = null;
            cla_api.get(req, function(error) {
                assert.equal(error, githubResponse.message);
                it_done();
            });
        });

        it('should handle error only if status unequal 200 or there is no response', function(it_done){
            githubResponse = {statusCode: 200, data: {}};
            githubError = 'any error';

            log.error.restore();
            sinon.stub(log, 'error', function(){
                assert();
            });

            cla_api.get(req, function(error, res) {

                assert(res);
                assert(!error);
                it_done();
            });
        });
    });


});

describe('cla api', function() {
    var req;
    beforeEach(function(){
        req = {
            user: {id: 3, login: 'login'},
            args: {
                repo: 'myRepo',
                owner: 'owner',
                gist: 'url/gistId'
            }
        };

        sinon.stub(repo_service, 'get', function(args, cb){
            assert(args);
            cb(null, {gist: 'url/gistId', token: 'abc'});
        });
        sinon.stub(github, 'direct_call', function(args, cb){
            assert(args.url);
            assert(args.token);
            assert.equal(args.url, url.githubPullRequests('owner', 'myRepo', 'open'));

            cb(null, {data: [{number: 1}, {number: 2}]});
        });

        sinon.stub(statusService, 'update', function(args){
            assert(args.signed);
        });
        sinon.stub(cla, 'sign', function(args, cb){
            assert.deepEqual(args, {repo: 'myRepo', owner: 'owner', user: 'login', user_id: 3});
            cb(null, 'done');
        });
        sinon.stub(cla, 'check', function(args, cb){
            cb(null, true);
        });
        sinon.stub(prService, 'editComment', function(){});
    });

    afterEach(function(){
        statusService.update.restore();
        repo_service.get.restore();
        github.direct_call.restore();
        cla.check.restore();
        cla.sign.restore();
        prService.editComment.restore();
    });

    it('should call cla service on sign', function(it_done){

        cla_api.sign(req, function(err){
            assert.ifError(err);
            assert(cla.sign.called);

            it_done();
        });
    });

    it('should update status of pull request created by user, who signed', function(it_done){
        cla_api.sign(req, function(error, res) {
            assert.ifError(error);
            assert.ok(res);
            assert(statusService.update.called);

            it_done();
        });
    });

    it('should update status of all open pull requests for the repo', function(it_done){
        cla_api.sign(req, function(error, res) {
            assert.ifError(error);
            assert.ok(res);
            assert.equal(statusService.update.callCount, 2);
            assert(github.direct_call.called);
            assert(prService.editComment.called);

            it_done();
        });
    });

    it('should comment with user_map if it is given', function(it_done){
        cla.check.restore();
        prService.editComment.restore();

        sinon.stub(cla, 'check', function(args, cb){
            cb(null, true, {signed: [], not_signed: []});
        });
        sinon.stub(prService, 'editComment', function(args){
            assert(args.user_map.signed);
        });

        cla_api.sign(req, function(error, res) {
            assert.ifError(error);
            assert.ok(res);
            assert(github.direct_call.called);
            assert(statusService.update.called);
            assert(prService.editComment.called);
            it_done();
        });
    });

    it('should handle repos without open pull requests', function(it_done){
        github.direct_call.restore();
        sinon.stub(github, 'direct_call', function(args, cb){
            cb(null, {});
        });

        cla_api.sign(req, function(error, res) {
            assert.ifError(error);
            assert.ok(res);
            assert(github.direct_call.called);
            assert(!statusService.update.called);

            it_done();
        });
    });
});

describe('cla api', function() {
    var req;
    beforeEach(function(){
        req = {
            user: {id: 3, login: 'login'},
            args: {
                repo: 'myRepo',
                owner: 'owner',
                gist: 'url/gistId'
            }
        };
    });

    it('should call cla service on getLastSignature', function(it_done) {
        sinon.stub(cla, 'getRepo', function(args, cb){
            assert.deepEqual(args, {repo: 'myRepo', owner: 'owner'});
            cb(null, {gist: 'url', token: 'abc'});
        });
        sinon.stub(cla, 'getLastSignature', function(args, cb){
            assert.deepEqual(args, {repo: 'myRepo', owner: 'owner', user: 'login', gist_url: 'url'});
            cb(null, {});
        });

        req.args = {repo: 'myRepo', owner: 'owner'};

        cla_api.getLastSignature(req, function(err){
            assert.ifError(err);
            assert(cla.getLastSignature.called);

            cla.getLastSignature.restore();
            cla.getRepo.restore();
            it_done();
        });
    });

    it('should call cla service on getSignedCLA', function(it_done){
        sinon.stub(cla, 'getSignedCLA', function(args, cb){
            assert.deepEqual(args, {user: 'login'});
            cb(null, {});
        });

        req.args = {user: 'login'};

        cla_api.getSignedCLA(req, function(err){
          assert.ifError(err);
          assert(cla.getSignedCLA.called);

          cla.getSignedCLA.restore();
          it_done();
        });
    });

    it('should call cla service on check', function(it_done){
        sinon.stub(cla, 'check', function(args, cb){
            assert.deepEqual(args, {repo: 'myRepo', owner: 'owner', user: 'login'});
            cb(null, true);
        });

        cla_api.check(req, function(err){
            assert.ifError(err);
            assert(cla.check.called);

            cla.check.restore();
            it_done();
        });
    });

    it('should call cla service on getAll', function(it_done){
        req.args.gist = 'url/gistId/version2';
        sinon.stub(cla, 'getAll', function(args, cb){
            assert.deepEqual(args, {repo: 'myRepo', owner: 'owner', gist: 'url/gistId/version2'});
            cb(null, []);
        });

        cla_api.getAll(req, function(err){
            assert.ifError(err);
            assert(cla.getAll.called);

            cla.getAll.restore();
            it_done();
        });
    });

    it('should call cla service on getGist', function(it_done){
        req.args.gist = 'url/gistId/version2';
        sinon.stub(cla, 'getRepo', function(args, cb){
            cb(null, {token: 123, gist: 'url/gistId'});
        });
        sinon.stub(cla, 'getGist', function(args, cb){
            assert.deepEqual(args, {token: 123, gist: 'url/gistId/version2'});
            cb(null, {});
        });

        cla_api.getGist(req, function(err){
            assert.ifError(err);
            assert(cla.getGist.called);

            cla.getRepo.restore();
            cla.getGist.restore();
            it_done();
        });
    });

    it('should call cla service using user token, not repo token', function(it_done){
        req.args.gist = 'url/gistId/version2';
        req.user.token = 'user_token';
        sinon.stub(cla, 'getRepo', function(args, cb){
            cb(null, {token: 'repo_token', gist: 'url/gistId'});
        });
        sinon.stub(cla, 'getGist', function(args, cb){
            assert.deepEqual(args, {token: 'user_token', gist: 'url/gistId/version2'});
            cb(null, {});
        });

        cla_api.getGist(req, function(err){
            assert.ifError(err);
            assert(cla.getGist.called);

            cla.getRepo.restore();
            cla.getGist.restore();
            it_done();
        });
    });

    it('should call cla service getGist with user token even if repo is not linked anymore', function(it_done){
        req.args.gist = {gist_url: 'url/gistId', gist_version: 'version2'};
        req.user.token = 'user_token';
        sinon.stub(cla, 'getRepo', function(args, cb){
            cb('There is no repo.', null);
        });
        sinon.stub(cla, 'getGist', function(args, cb){
            assert.deepEqual(args, {token: 'user_token', gist: {gist_url: 'url/gistId', gist_version: 'version2'}});
            cb(null, {});
        });

        cla_api.getGist(req, function(err){
            assert.ifError(err);
            assert(cla.getGist.called);

            cla.getRepo.restore();
            cla.getGist.restore();
            it_done();
        });
    });

    it('should fail calling cla service getGist with user token even if repo is not linked anymore when no gist is provided', function(it_done){
        req.args.gist = undefined;
        req.user.token = 'user_token';
        sinon.stub(cla, 'getRepo', function(args, cb){
            cb('There is no repo.', null);
        });
        sinon.stub(cla, 'getGist', function(args, cb){
            cb(null, {});
        });

        cla_api.getGist(req, function(err){
            assert(err);
            assert(!cla.getGist.called);

            cla.getRepo.restore();
            cla.getGist.restore();
            it_done();
            req.args.gist = 'url/gistId/version2';
        });
    });

});
