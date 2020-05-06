require 'spec_helper'
require 'puppet_labs/github/comment'

describe 'PuppetLabs::Github::Comment' do
  subject { PuppetLabs::Github::Comment.new }
  let(:payload) { read_fixture("example_comment.json") }
  let(:data)    { JSON.load(payload) }

  it 'creates a new instance using the from_json class method' do
    PuppetLabs::Github::Comment.from_json(payload)
  end

  it 'initializes with json' do
    comment = PuppetLabs::Github::Comment.new(:json => payload)
    comment.action.should == "created"
  end

  describe '#load_json' do
    it 'loads a json hash readable through the data method' do
      subject.load_json(payload)
      subject.action.should == "created"
    end
  end

  describe "#action" do
    actions = [ "created" ]
    payloads = [
      read_fixture("example_comment.json")
    ]

    actions.zip(payloads).each do |action, payload|
      it "returns '#{action}' when the issue is #{action}." do
        subject.load_json(payload)
        subject.action.should == action
      end
    end
  end

  describe "#issue" do
    subject { PuppetLabs::Github::Comment.new(:json => payload) }

    it 'is an instance of PuppetLabs::Github::Issue' do
      subject.issue.should be_a_kind_of PuppetLabs::Github::Issue
    end
  end

  describe "#pull_request" do
    subject { PuppetLabs::Github::Comment.new(:json => payload) }

    it 'is an instance of PuppetLabs::Github::PullRequest' do
      expect(subject.pull_request.instance_of?(PuppetLabs::Github::PullRequest)).to be
    end
  end

  describe "#repo_name" do
    subject { PuppetLabs::Github::Comment.new(:json => payload) }

    it 'delegates from the issue' do
      expect(subject.repo_name).to eq subject.issue.repo_name
    end
  end

  describe "#pull_request?" do
    subject { PuppetLabs::Github::Comment.new(:json => payload) }

    context 'the comment was on a pull request' do
      it 'returns true' do
        expect(subject.pull_request?).to be
      end
    end

    context 'the comment was on an issue' do
      before :each do
        subject.pull_request.stub(:html_url)
      end

      it 'returns false' do
        expect(subject.pull_request?).to_not be
      end
    end
  end

  context 'newly created comment' do
    subject { PuppetLabs::Github::Comment.new(:json => payload) }

    it 'has a body' do
      subject.body.should == data['comment']['body']
    end

    it 'has an author login' do
      subject.author_login.should == data['sender']['login']
    end

    it 'has an author avatar url' do
      subject.author_avatar_url.should == data['sender']['avatar_url']
    end

    it 'has a full name' do
      subject.full_name.should == data['repository']['full_name']
    end
  end
end
