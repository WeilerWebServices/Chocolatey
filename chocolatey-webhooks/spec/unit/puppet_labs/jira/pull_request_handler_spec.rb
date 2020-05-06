require 'spec_helper'
require 'puppet_labs/jira/pull_request_handler'

describe PuppetLabs::Jira::PullRequestHandler do

  include_context "Github pull request fixture"

  before :each do
    # Stub logging
    subject.stub(:logger).and_return(double.as_null_object)

    subject.pull_request = pr
    subject.stub(:project).and_return 'TEST'

    github_client = double('PuppetLabs::Github::GithubAPI', :account => github_account)
    pr.stub(:github).and_return github_client
  end

  {
    'opened' => PuppetLabs::Jira::Event::PullRequest::Open,
    'closed' => PuppetLabs::Jira::Event::PullRequest::Close,
    'reopened' => PuppetLabs::Jira::Event::PullRequest::Reopen,
  }.each_pair do |action, delegate|
    it "calls #{delegate}.perform when the #{action} action is received" do
      expect(pr).to receive(:action).at_least(:once).and_return action
      expect(delegate).to receive(:perform)

      subject.perform
    end
  end

  it "logs a warning when an unrecognized action is called"
end
