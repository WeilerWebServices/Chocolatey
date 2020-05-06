require 'puppet_labs/github/controller'
require 'puppet_labs/github/pull_request_controller'
require 'puppet_labs/github/issue_controller'
require 'puppet_labs/github/comment_controller'

module PuppetLabs
module Github
class GithubController < Controller
  ##
  # event_controller returns an instance of a controller class suitable for running.
  # If no controller is registered for the Github event then `nil` is returned.
  #
  # This method maps the X-Github-Event HTTP header onto a subclass of the
  # PuppetLabs::Controller base class.
  #
  # @return [PuppetLabs::Controller] subclass instance suitable to send the run
  # message to, or `nil`.
  def event_controller
    case gh_event = request.env['HTTP_X_GITHUB_EVENT'].to_s
    when 'pull_request'
      logger.info "Handling X-Github-Event (pull_request): #{gh_event}"
      pull_request = PuppetLabs::Github::PullRequest.from_json(route.payload)
      options = @options.merge({
        :pull_request => pull_request
      })
      controller = PuppetLabs::Github::PullRequestController.new(options)
      return controller
    when 'issues'
      logger.info "Handling X-Github-Event (issues): #{gh_event}"
      issue = PuppetLabs::Github::Issue.from_json(route.payload)
      options = @options.merge({
        :issue => issue
      })
      controller = PuppetLabs::Github::IssueController.new(options)
      return controller
    when 'issue_comment'
      logger.info "Handling X-Github-Event (comments): #{gh_event}"
      comment = PuppetLabs::Github::Comment.from_json(route.payload)
      options = @options.merge({
        :comment => comment
      })
      controller = PuppetLabs::Github::CommentController.new(options)
      return controller
    else
      logger.info "Ignoring X-Github-Event: #{gh_event}"
      return nil
    end
  end
end
end
end
