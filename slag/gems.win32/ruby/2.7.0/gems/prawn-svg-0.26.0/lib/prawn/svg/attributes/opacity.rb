module Prawn::SVG::Attributes::Opacity
  def parse_opacity_attributes_and_call
    # We can't do nested opacities quite like the SVG requires, but this is close enough.
    fill_opacity = stroke_opacity = clamp(properties.opacity.to_f, 0, 1) if properties.opacity
    fill_opacity = clamp(properties.fill_opacity.to_f, 0, 1) if properties.fill_opacity
    stroke_opacity = clamp(properties.stroke_opacity.to_f, 0, 1) if properties.stroke_opacity

    if fill_opacity || stroke_opacity
      state.fill_opacity *= fill_opacity || 1
      state.stroke_opacity *= stroke_opacity || 1

      add_call_and_enter 'transparent', state.fill_opacity, state.stroke_opacity
    end
  end
end
