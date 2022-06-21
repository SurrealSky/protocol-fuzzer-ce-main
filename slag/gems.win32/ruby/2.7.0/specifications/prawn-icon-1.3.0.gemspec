# -*- encoding: utf-8 -*-
# stub: prawn-icon 1.3.0 ruby lib

Gem::Specification.new do |s|
  s.name = "prawn-icon".freeze
  s.version = "1.3.0"

  s.required_rubygems_version = Gem::Requirement.new(">= 1.3.6".freeze) if s.respond_to? :required_rubygems_version=
  s.require_paths = ["lib".freeze]
  s.authors = ["Jesse Doyle".freeze]
  s.date = "2016-10-15"
  s.description = "  Prawn::Icon provides various icon fonts including\n  FontAwesome, Foundation Icons and GitHub Octicons\n  for use with the Prawn PDF toolkit.\n".freeze
  s.email = ["jdoyle@ualberta.ca".freeze]
  s.homepage = "https://github.com/jessedoyle/prawn-icon/".freeze
  s.licenses = ["RUBY".freeze, "GPL-2".freeze, "GPL-3".freeze]
  s.required_ruby_version = Gem::Requirement.new(">= 1.9.3".freeze)
  s.rubygems_version = "3.1.6".freeze
  s.summary = "Provides icon fonts for PrawnPDF".freeze

  s.installed_by_version = "3.1.6" if s.respond_to? :installed_by_version

  if s.respond_to? :specification_version then
    s.specification_version = 4
  end

  if s.respond_to? :add_runtime_dependency then
    s.add_runtime_dependency(%q<prawn>.freeze, [">= 1.1.0", "< 3.0.0"])
    s.add_development_dependency(%q<pdf-inspector>.freeze, ["~> 1.2.1"])
    s.add_development_dependency(%q<rspec>.freeze, ["~> 3.5.0"])
    s.add_development_dependency(%q<rubocop>.freeze, ["~> 0.44.1"])
    s.add_development_dependency(%q<rake>.freeze, [">= 0"])
    s.add_development_dependency(%q<pdf-reader>.freeze, ["~> 1.4"])
    s.add_development_dependency(%q<simplecov>.freeze, ["~> 0.12"])
  else
    s.add_dependency(%q<prawn>.freeze, [">= 1.1.0", "< 3.0.0"])
    s.add_dependency(%q<pdf-inspector>.freeze, ["~> 1.2.1"])
    s.add_dependency(%q<rspec>.freeze, ["~> 3.5.0"])
    s.add_dependency(%q<rubocop>.freeze, ["~> 0.44.1"])
    s.add_dependency(%q<rake>.freeze, [">= 0"])
    s.add_dependency(%q<pdf-reader>.freeze, ["~> 1.4"])
    s.add_dependency(%q<simplecov>.freeze, ["~> 0.12"])
  end
end
