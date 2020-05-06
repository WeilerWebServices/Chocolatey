require 'puppet_labs/jira/event/pull_request'
require 'puppet_labs/jira/event/pull_request/base'

require 'puppet_labs/jira/issue'
require 'puppet_labs/jira/formatter'

# Orchestrate the actions needed to open a new pull request.
#
# @api private
class PuppetLabs::Jira::Event::PullRequest::Open < PuppetLabs::Jira::Event::PullRequest::Base

  include PuppetLabs::Jira::Client

  def perform
    create_or_link
  end

  private

  def create_or_link
    identifier = pull_request.identifier
    title      = pull_request.title

    if (issue = issue_by_id(identifier))
      logger.debug "Pull request with id #{pull_request.identifier} already exists as #{issue.key}"
    elsif (issue = issue_by_title(title))
      link_issue(issue)
    else
      create_issue
    end
  end

  def link_issue(jira_issue)
    logger.info "Adding pull request link to issue #{jira_issue.key}"

    link_title = "Pull Request: #{pull_request.title}"
    link_icon  = {
      'url16x16' => 'http://github.com/favicon.ico',
      'title'    => 'Pull Request',
    }

    jira_issue.remotelink(
      pull_request.html_url,
      link_title,
      'Github',
      link_icon
    )
  end

  # Generate a new Jira issue based on the given pull request, and attach
  # a link to the pull request
  def create_issue
    logger.info "Creating new issue in project #{project.jira_project}: #{pull_request.title}"

    jira_issue = PuppetLabs::Jira::Issue.build(client, project)
    fields = PuppetLabs::Jira::Formatter.format_pull_request(pull_request)

    jira_issue.create(fields[:summary], fields[:description])
    link_issue(jira_issue)

    logger.info "Created jira issue with webhook-id #{pull_request.identifier}"
  rescue PuppetLabs::Jira::APIError => e
    logger.error "Failed to save #{pull_request.title}: #{e.message}"
  end
end
