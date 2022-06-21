# -*- encoding: utf-8 -*-
# stub: prawn-svg 0.26.0 ruby lib

Gem::Specification.new do |s|
  s.name = "prawn-svg".freeze
  s.version = "0.26.0"

  s.required_rubygems_version = Gem::Requirement.new(">= 0".freeze) if s.respond_to? :required_rubygems_version=
  s.require_paths = ["lib".freeze]
  s.authors = ["Roger Nesbitt".freeze]
  s.date = "2016-12-21"
  s.description = "This gem allows you to render SVG directly into a PDF using the 'prawn' gem.  Since PDF is vector-based, you'll get nice scaled graphics if you use SVG instead of an image.".freeze
  s.email = "roger@seriousorange.com".freeze
  s.homepage = "http://github.com/mogest/prawn-svg".freeze
  s.licenses = ["MIT".freeze]
  s.required_ruby_version = Gem::Requirement.new(">= 2.0.0".freeze)
  s.rubygems_version = "3.1.6".freeze
  s.summary = "SVG renderer for Prawn PDF library".freeze

  s.installed_by_version = "3.1.6" if s.respond_to? :installed_by_version

  if s.respond_to? :specification_version then
    s.specification_version = 4
  end

  if s.respond_to? :add_runtime_dependency then
    s.add_runtime_dependency(%q<prawn>.freeze, [">= 0.11.1", "< 3"])
    s.add_runtime_dependency(%q<css_parser>.freeze, ["~> 1.3"])
    s.add_development_dependency(%q<rspec>.freeze, ["~> 3.0"])
    s.add_development_dependency(%q<rake>.freeze, ["~> 10.1"])
  else
    s.add_dependency(%q<prawn>.freeze, [">= 0.11.1", "< 3"])
    s.add_dependency(%q<css_parser>.freeze, ["~> 1.3"])
    s.add_dependency(%q<rspec>.freeze, ["~> 3.0"])
    s.add_dependency(%q<rake>.freeze, ["~> 10.1"])
  end
end
