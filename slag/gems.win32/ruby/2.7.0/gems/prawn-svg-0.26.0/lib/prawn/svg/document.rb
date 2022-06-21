class Prawn::SVG::Document
  Error = Class.new(StandardError)
  InvalidSVGData = Class.new(Error)

  DEFAULT_FALLBACK_FONT_NAME = "Times-Roman"

  # An +Array+ of warnings that occurred while parsing the SVG data.
  attr_reader :warnings

  attr_reader :root,
    :sizing,
    :fallback_font_name,
    :font_registry,
    :url_loader,
    :css_parser, :elements_by_id, :gradients

  def initialize(data, bounds, options, font_registry: nil, css_parser: CssParser::Parser.new)
    @root = REXML::Document.new(data).root

    if @root.nil?
      if data.respond_to?(:end_with?) && data.end_with?(".svg")
        raise InvalidSVGData, "The data supplied is not a valid SVG document.  It looks like you've supplied a filename instead; use IO.read(filename) to get the data before you pass it to prawn-svg."
      else
        raise InvalidSVGData, "The data supplied is not a valid SVG document."
      end
    end

    @warnings = []
    @options = options
    @elements_by_id = {}
    @gradients = {}
    @fallback_font_name = options.fetch(:fallback_font_name, DEFAULT_FALLBACK_FONT_NAME)
    @font_registry = font_registry
    @css_parser = css_parser

    @url_loader = Prawn::SVG::UrlLoader.new(
      enable_cache:          options[:cache_images],
      enable_web:            options.fetch(:enable_web_requests, true),
      enable_file_with_root: options[:enable_file_requests_with_root]
    )

    @sizing = Prawn::SVG::Calculators::DocumentSizing.new(bounds, @root.attributes)
    calculate_sizing(requested_width: options[:width], requested_height: options[:height])

    parse_style_elements

    yield self if block_given?
  end

  def calculate_sizing(requested_width: nil, requested_height: nil)
    sizing.requested_width = requested_width
    sizing.requested_height = requested_height
    sizing.calculate
  end

  private

  # <style> elements specified anywhere in the document apply to the entire
  # document.  Because of this, we load all <style> elements before parsing
  # the rest of the document.
  def parse_style_elements
    REXML::XPath.match(root, '//style').each do |source|
      data = source.texts.map(&:value).join
      css_parser.add_block!(data)
    end
  end
end
