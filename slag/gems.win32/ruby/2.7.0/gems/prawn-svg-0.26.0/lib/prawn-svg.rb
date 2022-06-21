require 'rexml/document'

require 'prawn'
require 'prawn/svg/version'

require 'css_parser'

require 'prawn/svg/font_registry'
require 'prawn/svg/calculators/arc_to_bezier_curve'
require 'prawn/svg/calculators/aspect_ratio'
require 'prawn/svg/calculators/document_sizing'
require 'prawn/svg/calculators/pixels'
require 'prawn/svg/url_loader'
require 'prawn/svg/loaders/data'
require 'prawn/svg/loaders/file'
require 'prawn/svg/loaders/web'
require 'prawn/svg/color'
require 'prawn/svg/attributes'
require 'prawn/svg/properties'
require 'prawn/svg/pathable'
require 'prawn/svg/elements'
require 'prawn/svg/extension'
require 'prawn/svg/interface'
require 'prawn/svg/css'
require 'prawn/svg/ttf'
require 'prawn/svg/font'
require 'prawn/svg/document'
require 'prawn/svg/state'

module Prawn
  Svg = SVG # backwards compatibility
end

Prawn::Document.extensions << Prawn::SVG::Extension
