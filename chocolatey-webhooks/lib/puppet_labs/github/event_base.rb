require 'json'
require 'digest/md5'

module PuppetLabs
  module Github

    # Define shared behaviors of Github events
    #
    # @abstract
    #
    # @api private
    #
    # @see http://developer.github.com/v3/activity/events/types/
    class EventBase

      # Generate a object from the JSON representation.
      #
      # @param json [String] The JSON data to turn into an object
      #
      # @return [Object] An instance of the class with the properties set from
      #   the JSON data.
      def self.from_json(json)
        new(:json => json)
      end

      # Generate a new object.
      #
      # @param options [Hash<Symbol, Object>] A hash of named parameters used
      #   for object initialization
      #
      # @option options [String] :json A string containing a JSON representation
      #   of the object
      def initialize(options = {})
        if (json = options[:json])
          load_json(json)
        end
      end

      # @!attribute [r] action
      #   @return [String] The action represented in the event
      attr_reader :action

      # @!attribute [r] repo_name
      #   @return [String] The name of the github repository. (Sans the username)
      #   @example
      #     event.repo_name #=> "puppet-webhooks"
      attr_reader :repo_name

      # @!attribute [r] full_name
      #   @return [String] The full name of the repository
      #   @example
      #     event.name #=> "puppetlabs/puppet-webhooks"
      attr_reader :full_name

      # @!attribute [r] body
      #   @return [String] The textual body of the event.
      attr_reader :body

      # @!attribute [r] raw
      #   @return [Hash] The raw parsed data from the JSON message.
      #   @api private
      attr_reader :raw

      # Parse a JSON string and set the attributes on this object accordingly.
      #
      # @param json [String] The serialized JSON data to parse
      def load_json(json)
        @raw = JSON.load(json)

        @action = @raw['action']
      end

      # Define a unique identifier for this event.
      #
      # @return [String] A unique string representing this event.
      def identifier
        seed = "#{@full_name}:#{@number}"
        Digest::MD5.hexdigest(seed)
      end
    end
  end
end
